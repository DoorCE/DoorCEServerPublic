using System.Reflection;
using System.Runtime;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoorCEServer.Application;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Infrastructure;
using DoorCEServer.Tests;
using DoorCEServer.Utils;
using DoorCEServer.WebApi;
using DoorCEServer.WebApi.Controllers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;

namespace DoorCEServer;

public class ExcludeControllerConvention(Type controllerType) : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.ControllerType == controllerType)
        {
            controller.ApiExplorer.IsVisible = false;
            controller.Selectors.Clear();
        }
    }
}

public class ExcludeControllerFeatureProvider(Type controllerType)
    : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        feature.Controllers.Remove(controllerType.GetTypeInfo());
    }
}

public class Program
{
    // Response for the health check endpoint
    private static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonWriterOptions { Indented = true };

        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            jsonWriter.WriteStartObject("results");

            foreach (var healthReportEntry in healthReport.Entries)
            {
                jsonWriter.WriteStartObject(healthReportEntry.Key);
                jsonWriter.WriteString("status", healthReportEntry.Value.Status.ToString());
                jsonWriter.WriteString("description", healthReportEntry.Value.Description);
                jsonWriter.WriteStartObject("data");

                foreach (var item in healthReportEntry.Value.Data)
                {
                    jsonWriter.WritePropertyName(item.Key);

                    JsonSerializer.Serialize(jsonWriter, item.Value,
                        item.Value.GetType());
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        return context.Response.WriteAsync(
            Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
    
    private static void WriteStartupInformation(bool debugState, string version)
    {
        string? runtimeVersion = (Assembly.GetEntryAssembly() 
                                  ?? throw new InvalidOperationException())
                                    .GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        string osNameAndVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        string machineName = Environment.MachineName;

        bool isServerGc = GCSettings.IsServerGC;
        GCLargeObjectHeapCompactionMode largeObjectHeapCompactionMode = GCSettings.LargeObjectHeapCompactionMode;
        GCLatencyMode latencyMode = GCSettings.LatencyMode;
        string nodeId = SystemInfo.GetNodeId();
        string ip = SystemInfo.GetIpAddress();

        Console.Title = string.Format($"UDAS Server - Machine: {machineName} - Node: {nodeId}");
        Log.Information("Starting UDAS Server version: {Version}", version);
        Log.Information("Host machine: {Machine}, id: {Id}, ip: {Ip}", machineName, nodeId, ip);
        Log.Information("Runtime Version: {Version}, OS: {OS}", runtimeVersion, osNameAndVersion);
        Log.Information("Runtime Settings: GC Server: {IsGCServer}, GC LOH Mode: {LOHCompact}, GC Latency Mode: {LatencyMode}",
            isServerGc, largeObjectHeapCompactionMode, latencyMode);
        Log.Information("<Debugging> mode: {Mode}", debugState ? "Yes" : "No");
    }
    
    // Call on program exit
    private static void OnExit(object? sender, EventArgs eventArgs)
    {
        Log.CloseAndFlush();
    }
    
    public static void Main(string[] args)
    {
        // Set up the server version
        // IMPORTANT: changes with every build, so do not use for anything else than display/logging purposes
        // format: YYYYMMDD-BUILDNUMBER, where BUILDNUMBER is incremented for each build on the same day
        string version = Assembly.GetEntryAssembly()
                             ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                             ?.InformationalVersion
                         ?? "unknown";
        
        // Define standard application behaviour for unhandled exceptions and on exiting
        AppDomain.CurrentDomain.UnhandledException += (_, arguments) =>
        {
            var ex = (Exception)arguments.ExceptionObject;
            if (ex.GetType() == typeof(SystemHaltException))
            {
                Log.Fatal("System terminated: {ExMessage}", ex.Message);
                Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(ex));
            }
            Log.Error("Unhandled Exception: {Ex}", ex.ToString());
            Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(ex));
        };
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        
        // Define the application builder and determine the environment type (dev vs. prod)
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        // builder.WebHost.ConfigureKestrel(options =>
        // {
        //     options.ListenAnyIP(8443, listenOptions =>
        //     {
        //         listenOptions.UseHttps("aspnetcore.pfx", "pass");
        //     });
        // });
        string environmentName = builder.Environment.IsDevelopment() ? "Development" : "Production";

        // Gather the configuration parameters from environment variables, appsettings files and command line arguments
        string contentRootPath = Directory.GetCurrentDirectory();
        IConfigurationRoot configuration = builder.Configuration
            .SetBasePath(contentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddJsonFile($"appsettings.local.json", optional: true)
            .AddCommandLine(args)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>().Build();
        
        // TODO - check if configuration contains all required variables (e.g. CKAN token) and throw exceptions in case
        
        // Create and configure the standard static logger and use it in Serilog service
        bool.TryParse(configuration["Debug"], out bool debugState);
        var loggingLevelSwitch = new LoggingLevelSwitch {
            MinimumLevel = debugState ? LogEventLevel.Debug : LogEventLevel.Information
        };
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)                    
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)                    
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(contentRootPath + "/Logs/log-.txt", 
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 4194304)
            .CreateLogger();
        builder.Services.AddSerilog();
        
        // Write to the log basic information about the server at startup
        WriteStartupInformation(debugState, version);
        
        // Add services to the Dependency Injection container.
        builder.AddInfrastructureServices();
        builder.AddWebApiServices();
        builder.AddApplicationServices(version);
        builder.AddPopulateServices();
        if (builder.Environment.IsDevelopment()) {
            builder.AddTestServices(); // register test services only in Development mode
        }
        
        // Add Keycloak authentication
        builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        
        // Configure API controllers
        builder.Services.AddControllers(options =>
            {
                options.Filters.Add<HttpResponseExceptionFilter>();
                // Exclude the MaintenanceController from the API in production mode
                if (!builder.Environment.IsDevelopment()) {
                    options.Conventions.Add(new ExcludeControllerConvention(typeof(MaintenanceController)));
                }
            })
            .AddJsonOptions(o => {
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.JsonSerializerOptions.IncludeFields = true;
            })
            .ConfigureApplicationPartManager(manager =>
            {
                if (!builder.Environment.IsDevelopment()) {
                    // Completely removes MaintenanceController from discovery if not in Development mode
                    manager.FeatureProviders.Add(
                        new ExcludeControllerFeatureProvider(typeof(MaintenanceController)));
                }
            });

        // Add Swagger service only in Development mode
        if (builder.Environment.IsDevelopment()) {
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "UDAS Server API",
                    Version = version,
                    Description = "Universal Data Acquisition System Server REST API"
                });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter JWT token (just the token)",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            });
        }

        // Add API controller with authorisation and CORS policies
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: "CorsPolicy",
                cpb => cpb.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });
        var app = builder.Build();
        app.UseCors("CorsPolicy");
        // app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        
        // Add health check endpoint with custom response writer
        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = WriteResponse
        });

        // Use OpenAPI and Swagger only in Development mode
        if (app.Environment.IsDevelopment()) {
            app.UseSwagger(options =>
            {
                options.PreSerializeFilters.Add((swaggerDoc, httpRequest) => {
                    swaggerDoc.Servers = new List<OpenApiServer> {
                        new OpenApiServer {
                            Url = Environment.GetEnvironmentVariable("UDAS_EXTERNAL_URL")
                                  ?? $"{httpRequest.Scheme}://{httpRequest.Host.Value}"
                        }
                    };
                });
            });
            app.UseSwaggerUI();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("UDAS Server API")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDarkMode(true)
                    .WithDefaultHttpClient(ScalarTarget.Dart, ScalarClient.Http)
                    .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
                    
            });
        }
        
        // Initiate database
        using (var scope = app.Services.CreateScope())
        {
            var dataInit = scope.ServiceProvider.GetRequiredService<DataInitialisation>();
            dataInit.InitiateDatabase();
            bool.TryParse(configuration["Demo"], out var demoState);
            Log.Information("<Demo> option: {demo}", demoState ? "Yes" : "No");
            if (demoState) dataInit.PopulateDemoDatabase();
        }
        
        app.Run();
    }
}