using DoorCEServer.Application.DataTemplateManager.Dtos;

namespace DoorCEServer.Application.DataTemplateManager.Interfaces;

public interface SchemaAPI
{
    // Schema Series operations
    IEnumerable<XSchemaSeries> GetAllowedSchemaSeries(string? schemaId, string userId);
    XSchemaSeries GetSchemaSeries(string identifier, string? userId);
    IEnumerable<XSchemaSeries> GetSchemaSeriesList(string? query, string? userId);
    string UpsertSchemaSeries(XSchemaSeries series, string userId);
    void DeleteSchemaSeries(string identifier, string userId);
    public bool CheckSchemaSeriesId(string identifier);
    
    // Data Schema operations
    IEnumerable<XDataSchema> GetDataSchemaList(string? query, string? userId);
    IEnumerable<XDataSchema> GetSeriesDataSchemaList(string seriesIdentifier, string? userId);
    XDataSchema GetDataSchema(string identifier, string? userId);
    string UpsertDataSchema(XDataSchema schema, string userId);
    void DeleteDataSchema(string identifier, string userId);
    public bool CheckSchemaId(string identifier);
    
    // Namespace operations
    IEnumerable<XNamespace> GetStandardNamespaceList();
}