using DoorCEModel.Infrastructure;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Interfaces;
using DoorCEServer.Tests.SchemaManager.Labels;
using DoorCEServer.Tests.SchemaManager.MockDtos;

namespace DoorCEServer.Tests.SchemaManager.DtoHelpers;

public class SchemaHelper(ApplicationDbContext context, SchemaAPI schemaApi, TestCommon testCommon)
{
    private readonly XSchemaMock _xSchemaMock = new();
    
    public XDataSchema? GetFromDb(SchemaLabel schemaLabel)
    {
        XDataSchema xSchema = _xSchemaMock.Get(schemaLabel, VariantLabel.AfterUpsert);
        try {
            return schemaApi.GetDataSchema(xSchema.Uri!, "udas-admin");
        } catch {
            return null;
        }
    }

    public void Upsert(SchemaLabel schemaLabel, VariantLabel variantLabel)
    {
        XDataSchema xSchema = _xSchemaMock.Get(schemaLabel, variantLabel);
        
        schemaApi.UpsertDataSchema(xSchema, "udas-admin");
    }

    public void Delete(SchemaLabel schemaLabel)
    {
        XDataSchema xSchema = _xSchemaMock.Get(schemaLabel, VariantLabel.AfterUpsert);
        schemaApi.DeleteDataSchema(xSchema.Uri!, "udas-admin");
    }
    
    public bool ValidateDelete(SchemaLabel schemaLabel)
    {
        XDataSchema schema = _xSchemaMock.Get(schemaLabel, VariantLabel.AfterUpsert);
        
        // check if series was deleted
        return !context.DataSchemas.Any(ds => ds.Uri == schema.Uri);
    }

    public bool Validate(XDataSchema xSchema, SchemaLabel schemaLabel, VariantLabel variantToCompare)
    {
        return testCommon.CheckXDataSchemaEquality(xSchema, _xSchemaMock.Get(schemaLabel, variantToCompare));
    }
}