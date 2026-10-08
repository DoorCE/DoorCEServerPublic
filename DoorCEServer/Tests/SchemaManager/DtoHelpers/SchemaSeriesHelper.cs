using DoorCEModel.Infrastructure;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Interfaces;
using DoorCEServer.Tests.SchemaManager.Labels;
using DoorCEServer.Tests.SchemaManager.MockDtos;

namespace DoorCEServer.Tests.SchemaManager.DtoHelpers;

public class SchemaSeriesHelper(ApplicationDbContext context, SchemaAPI schemaApi, TestCommon testCommon)
{
    private readonly XSchemaSeriesMock _xSSeriesMock = new();
    
    public XSchemaSeries? GetFromDb(SSeriesLabel seriesLabel)
    {
        XSchemaSeries xSeries = _xSSeriesMock.Get(seriesLabel, VariantLabel.AfterUpsert);
        try {
            return schemaApi.GetSchemaSeries(xSeries.Uri!, "udas-admin");
        } catch {
            return null;
        }
    }

    public void Upsert(SSeriesLabel seriesLabel, VariantLabel variantLabel)
    {
        XSchemaSeries xSeries = _xSSeriesMock.Get(seriesLabel, variantLabel);
        
        schemaApi.UpsertSchemaSeries(xSeries, "udas-admin");
    }

    public void Delete(SSeriesLabel seriesLabel)
    {
        XSchemaSeries xSeries = _xSSeriesMock.Get(seriesLabel, VariantLabel.AfterUpsert);
        schemaApi.DeleteSchemaSeries(xSeries.Uri!, "udas-admin");
    }
    
    public bool ValidateDelete(SSeriesLabel seriesLabel)
    {
        XSchemaSeries series = _xSSeriesMock.Get(seriesLabel, VariantLabel.AfterUpsert);
        
        // check if series was deleted
        return !context.SchemaSeries.Any(ss => ss.Uri == series.Uri);
    }

    public bool Validate(XSchemaSeries xSchemaSeries, SSeriesLabel seriesLabel, VariantLabel variantToCompare)
    {
        return testCommon.CheckSchemaSeriesEquality(xSchemaSeries, _xSSeriesMock.Get(seriesLabel, variantToCompare));
    }
}