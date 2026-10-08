using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DoorCEServer.Infrastructure.HealthChecks;

public class GCInfoHealthCheck : IHealthCheck
{
    public string Name { get; } = "GCInfo";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
    {            
        var allocatedMegaBytes = GC.GetTotalMemory(forceFullCollection: false) / 1000000; // divided to get MB
        var data = new Dictionary<string, object>()
        {
            { "allocatedMegaBytes", allocatedMegaBytes }
        };

        var status = (allocatedMegaBytes >= 200) ? HealthStatus.Unhealthy : HealthStatus.Healthy;

        return Task.FromResult(
            new HealthCheckResult(
                status,
                exception: null,
                description: $"Reports degraded status if allocated MB >= 200MB, current: {allocatedMegaBytes} MB",
                data: data
            )
        );
    }        
}