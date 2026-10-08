using System.Reflection;
using DoorCEGenerator.Application.Domain;
using DoorCEGenerator.Application.InfoManager.Domain;
using DoorCEGenerator.Application.InfoManager.Interfaces;
using DoorCEGenerator.Application.Interfaces;
using FluentValidation;

namespace DoorCEGenerator.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder, string version)
    {
        builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddScoped<AppGenerationAPI, MAppGeneration>();
        builder.Services.AddScoped<CodeProvisioningAPI, MCodeProvisioning>();
        builder.Services.AddScoped<InfoAPI>(_ => new MInfo(version));

    }
}