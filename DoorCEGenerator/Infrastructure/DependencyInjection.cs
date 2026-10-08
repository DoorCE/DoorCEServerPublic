using DoorCEGenerator.Common.Exceptions;
using DoorCEGenerator.Infrastructure.DataAccess;
using DoorCEModel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Serilog;

namespace DoorCEGenerator.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        
        var server = configuration["DB_SERVER"]??"localhost";
        var port = configuration["DB_PORT"]??"5432";
        var database = configuration["DB_DATABASE"]??"DoorCEServerTest";
        var username = configuration["DB_USERNAME"]??"postgres";
        var password = configuration["DB_PASSWORD"]??"password";
        // Database Service
        var connectionString = configuration.GetConnectionString("UDAS_DB_STRING") ??
                               $"Server={server};Port={port};Database={database};Username={username};Password={password};";
        if (connectionString == null) {
            throw new SystemHaltException("No connection string found in configuration.");
        }

        connectionString += "Pooling=false;";
        if (builder.Environment.IsDevelopment())
            connectionString += "Include Error Detail=true;"; // add error details in Development mode
        
        Log.Information("<Database> configuring with connection string: {ConnectionString}",
            connectionString.Replace(password, "****"));
        bool.TryParse(configuration["UsePostgis"], out var usePostgis);
        Log.Information("<Database> system use GIS: {usePostgis}", usePostgis ? "Yes" : "No");
        builder.Services.AddDbContext<ApplicationDbContext>(op =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            if (usePostgis) {
                dataSourceBuilder.UseNetTopologySuite();
            }
            op
                //.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
                .UseNpgsql(dataSourceBuilder.Build());
        });
        
        // Health Self Checks Service
        builder.Services
            .AddHealthChecks()
            .AddCheck(
                "self", () => HealthCheckResult.Healthy("Dynamic Config is OK!"),
                tags: ["self"]
            );
        
        builder.Services.AddScoped<ICodeRepository, MCodeRepository>();
        builder.Services.AddScoped<IDataTemplates,MDataTemplates>();
    }
}