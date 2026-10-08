using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEServer.Application.AppGenProxy.Dtos;
using DoorCEServer.Application.AppGenProxy.Interfaces;

namespace DoorCEServer.Application.AppGenProxy.Domain;

public class MAppGenProxy(IHttpClientFactory httpClientFactory) : IAppGen
{
    private HttpClient AppGenClient => httpClientFactory.CreateClient("AppGen");
    private HttpClient AppCompClient => httpClientFactory.CreateClient("AppComp");
    
    public void DeployApp(AcquisitionApp app)
    {
        AppDeploymentRequestDto request = new() {
            AppUri = app.Uri,
            TemplateUri = app.Template.Uri,
            ActiveResourceUri = app.ActiveResource.Uri,
            SourceResourceUris = app.SourceResources.Select(r => r.Uri).ToList()
        };
        PostToAppCompiler(request).GetAwaiter().GetResult();
    }

    public string GenerateCodeFromTemplate(string templateId, string codeFramework)
    {
        return PostToAppGenerator(templateId, codeFramework).GetAwaiter().GetResult();
    }

    public string? GetVersion()
    {
        try {
            using HttpResponseMessage response = AppGenClient.GetAsync("/info/GetVersion").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        } catch (Exception) {
            return null;
        }
    }

    private async Task PostToAppCompiler(AppDeploymentRequestDto request)
    {
        using HttpResponseMessage response = await AppCompClient.PostAsync("DeployApp", JsonContent.Create(request));
        response.EnsureSuccessStatusCode();
    }
    
    private async Task<string> PostToAppGenerator(string templateId, string codeFramework)
    {
        using HttpResponseMessage response = await AppGenClient
            .PostAsync($"/generate/GenerateCodeFromTemplate?templateId={templateId}&codeFramework={codeFramework}",
                null);
        response.EnsureSuccessStatusCode();
        string codePackageUri = await response.Content.ReadAsStringAsync() 
                                ?? throw new Exception("Could not get code package URI when generating App");
        return codePackageUri;
    }
}