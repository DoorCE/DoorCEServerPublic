using System.Reflection;
using DoorCEServer.Application.AppGenProxy.Domain;
using DoorCEServer.Application.AppGenProxy.Interfaces;
using DoorCEServer.Application.CataloguingManager.Common;
using DoorCEServer.Application.CataloguingManager.Domain;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Application.CkanProxy.Domain;
using DoorCEServer.Application.CkanProxy.Domain.Services;
using DoorCEServer.Application.CkanProxy.Interfaces;
using DoorCEServer.Application.DataContentsManager.Domain;
using DoorCEServer.Application.DataContentsManager.Interfaces;
using DoorCEServer.Application.DataTemplateManager.Common;
using DoorCEServer.Application.DataTemplateManager.Domain;
using DoorCEServer.Application.DataTemplateManager.Interfaces;
using DoorCEServer.Application.InfoManager.Domain;
using DoorCEServer.Application.InfoManager.Interfaces;
using DoorCEServer.Application.KeycloakAdminProxy.Domain;
using DoorCEServer.Application.KeycloakAdminProxy.Interfaces;
using DoorCEServer.Application.StorageManager.Domain;
using DoorCEServer.Application.StorageManager.Interfaces;
using DoorCEServer.Application.VocabularyManager.Domain;
using DoorCEServer.Application.VocabularyManager.Interfaces;
using FluentValidation;

namespace DoorCEServer.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder, string version)
    {
        builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddScoped<MCataloguingManagerCommon>();
        builder.Services.AddScoped<MAgentsCommon>();
        builder.Services.AddScoped<MSchemaManagerCommon>();
        builder.Services.AddScoped<AgentAPI, MAgents>();
        builder.Services.AddScoped<DataAcquisitionAPI, MDataAcquisition>();
        builder.Services.AddScoped<DatasetManagementAPI, MDatasetManagement>();
        builder.Services.AddScoped<DistributionManagementAPI, MDistributionManagement>();
        builder.Services.AddScoped<SchemaAPI, MSchemas>();
        builder.Services.AddScoped<MAppTemplates>();
        builder.Services.AddScoped<AppTemplateAPI>(sp => sp.GetRequiredService<MAppTemplates>());
        builder.Services.AddScoped<IAppTemplates>(sp => sp.GetRequiredService<MAppTemplates>());
        builder.Services.AddScoped<VocabularyAPI, MVocabulary>();
        builder.Services.AddScoped<StorageAPI, MStorage>();
        builder.Services.AddScoped<DataInitialisation>();
        builder.Services.AddScoped<InfoAPI>(sp => new MInfo(version, sp.GetRequiredService<IAppGen>()));

        // Http client and service for Data Upload
        builder.Services.AddHttpClient("FileDownload", _ => { })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 3
            }); // register a named HttpClient for file upload in MDataUpload
        builder.Services.AddScoped<DataUploadAPI, MDataUpload>();

        // Http client and service for CKAN
        builder.Services.AddHttpClient("Ckan", (sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(
                (cfg["CKAN_BASE_ADDRESS"] 
                 ?? throw new Exception("No CKAN base address in configuration")) + "/");
            client.DefaultRequestHeaders.Add(
                "Authorization",
                cfg["CKAN_AUTHORIZATION_TOKEN"] ??
                throw new  Exception("No CKAN authorization token provided in configuration"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        builder.Services.AddScoped<ICkanActions,MCkanActions>();
        builder.Services.AddScoped<CkanClient>();
        builder.Services.AddScoped<RequestBuilder>();
        builder.Services.AddScoped<CkanDatastoreService>();
        builder.Services.AddScoped<CkanMetadataService>();

        // Http client and service for KeycloakAdmin
        builder.Services.AddHttpClient("KeycloakAdmin", (sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(
                cfg["Keycloak:AuthServerUrl"] ??
                throw new Exception("No Keycloak URL in configuration"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        builder.Services.AddSingleton<IKeycloakAdmin,MKeycloakAdmin>();

        // Http clients and service for AppGenProxy
        builder.Services.AddHttpClient("AppGen", (sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(cfg["UDAS_APPGEN_URL"] ??
                                         throw new Exception("No UDAS AppGen URL in configuration"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        builder.Services.AddHttpClient("AppComp", (sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(
                (cfg["UDAS_APPCMP_URL"] ??
                 throw new Exception("No UDAS AppComp URL in configuration")) + "/compile/");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        builder.Services.AddSingleton<IAppGen,MAppGenProxy>();
    }
}