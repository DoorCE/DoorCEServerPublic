using DoorCEServer.Utils.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace DoorCEServer.Infrastructure.HealthChecks;

public class NpgSqlHealthCheckOptions
{
    internal NpgSqlHealthCheckOptions()
    {
    }

    public NpgSqlHealthCheckOptions(string connectionString)
    {
        ConnectionString = Guard.ThrowIfNull(connectionString, throwOnEmptyString: true);
    }

    public NpgSqlHealthCheckOptions(NpgsqlDataSource dataSource)
    {
        DataSource = Guard.ThrowIfNull(dataSource);
    }

    public string? ConnectionString { get; internal set; }

    public NpgsqlDataSource? DataSource { get; internal set; }

    public string CommandText { get; set; } = NpgSqlHealthCheckBuilderExtensions.HEALTH_QUERY;

    public Action<NpgsqlConnection>? Configure { get; set; }

    public Func<object?, HealthCheckResult>? HealthCheckResultBuilder { get; set; }
}