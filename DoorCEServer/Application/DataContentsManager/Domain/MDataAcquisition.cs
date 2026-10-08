using System.Text.Json;
using System.Text.Json.Nodes;
using AutoMapper;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEModel.Utils.Extensions;
using DoorCEServer.Application.CataloguingManager.Domain;
using DoorCEServer.Application.DataContentsManager.Common;
using DoorCEServer.Application.DataContentsManager.Dtos;
using DoorCEServer.Application.DataContentsManager.Interfaces;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;
using Json.Schema;
using Microsoft.EntityFrameworkCore;
using JsonSchemaBuilder = DoorCEServer.Application.DataContentsManager.Domain.Services.JsonSchemaBuilder;

namespace DoorCEServer.Application.DataContentsManager.Domain;

public class MDataAcquisition(IMapper mapper, ApplicationDbContext dbContext, MAgentsCommon agents)
    : MDataManagement(agents), DataAcquisitionAPI
{
    public IEnumerable<XDataItem> GetDatasetItemList(string datasetUri, string? conceptUri, XDataFilterParams? criteria,
        string userId)
    {
        // TODO - criteria should be applied to the query, but for now it is ignored
        Dataset dataset = dbContext.Datasets
                              .Include(d => d.Schema)
                              .Include(d => d.Schema!.UsedNamespaces)
                              .Include(d => d.Schema!.Concepts)
                              .ThenInclude(c => c.Properties)
                              .Include(d => d.Schema!.Concepts)
                              .ThenInclude(c => c.Namespace)
                              .AsSplitQuery()
                              .FirstOrDefault(d => d.Uri == datasetUri) 
                          ?? throw new ArgumentException("Dataset not found");
        
        // ===> Authorisation
        CheckDataRetrievalAuthorisation(dataset, userId);
        
        if (null == dataset.Schema)
            throw new ArgumentException("Items can be retrieved only from datasets with schemas");
        string targetConceptUri = conceptUri ?? dataset.Schema!.MainConcept!.Uri 
                                 ?? throw new ArgumentException("Concept URI has to be provided if the dataset schema does not define main concept");
        Concept concept = dataset.Schema.Concepts.FirstOrDefault(c => c.Uri == targetConceptUri) ??
                          throw new ArgumentException("Concept not found in the dataset schema");
        List<DataItem> allDataItems = dbContext.DataItems
            .Include(d => d.Dataset)
            .Include(d => d.Concept)
            .AsSplitQuery()
            .Where(d => datasetUri == d.Dataset.Uri)
            .ToList();
        ICollection<XDataItem> result = mapper.Map<ICollection<DataItem>,ICollection<XDataItem>>(allDataItems)
            .Where(d => d.ConceptUri == targetConceptUri).ToList();
        foreach (XDataItem item in result)
            item.Values = MapOutgoingValues(item.Values, concept);
        return result;
    }

    public XDataItem GetDataItem(string identifier, string datasetUri, string? conceptUri, string userId)
    {
        Dataset dataset = dbContext.Datasets
                              .Include(d => d.Schema)
                              .Include(d => d.Schema!.UsedNamespaces)
                              .Include(d => d.Schema!.Concepts)
                              .ThenInclude(c => c.Properties)
                              .Include(d => d.Schema!.Concepts)
                              .ThenInclude(c => c.Namespace)
                              .AsSplitQuery()
                              .FirstOrDefault(d => d.Uri == datasetUri) 
                          ?? throw new ArgumentException("Dataset not found");
        
        // ===> Authorisation
        CheckDataRetrievalAuthorisation(dataset, userId);
        
        if (null == dataset.Schema)
            throw new ArgumentException("Items can be retrieved only from datasets with schemas");
        string targetConceptUri = conceptUri ?? dataset.Schema!.MainConcept!.Uri 
            ?? throw new ArgumentException("Concept URI has to be provided if the dataset schema does not define main concept");
        Concept concept = dataset.Schema.Concepts.FirstOrDefault(c => c.Uri == targetConceptUri) ??
                          throw new ArgumentException("Concept not found in the dataset schema");
        List<DataItem> allDataItems = dbContext.DataItems
            .Include(d => d.Dataset)
            .Include(d => d.Concept)
            .AsSplitQuery()
            .Where(d => datasetUri == d.Dataset.Uri)
            .ToList();
        XDataItem result = mapper.Map<DataItem,XDataItem>(allDataItems
                .SingleOrDefault(d => d.Identifier == identifier && d.ConceptUri == targetConceptUri)
                                              ?? throw new ArgumentException("DataItem not found"));
        result.Values = MapOutgoingValues(result.Values, concept);
        return result;
    }

    public void UpsertDataItem(XDataItem xDataItem, string userId)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        try
        {
            Dataset dataset = dbContext.Datasets
                                  .Include(d => d.Schema)
                                  .Include(d => d.Schema!.UsedNamespaces)
                                  .Include(d => d.Schema!.Concepts)
                                  .ThenInclude(c => c.Namespace)
                                  .Include(d => d.Schema!.Concepts)
                                  .ThenInclude(c => c.Properties)
                                  .Include(d => d.Items)
                                  .AsSplitQuery()
                                  .FirstOrDefault(d => d.Uri == xDataItem.DataSetUri)
                              ?? throw new ArgumentException("Dataset not found");
            
            // ===> Authorisation
            CheckDataModificationAuthorisation(dataset, userId);
            
            if (null == dataset.Schema)
                throw new ArgumentException("Items can be added only to datasets with schemas");
            Concept concept = !string.IsNullOrEmpty(xDataItem.ConceptUri)
                ? dataset.Schema.Concepts.FirstOrDefault(c => c.Uri == xDataItem.ConceptUri)
                  ?? throw new ArgumentException("Concept not found in the dataset schema")
                : dataset.Schema.MainConcept ??
                  throw new ArgumentException("Concept not found in the dataset schema");
            string? identifier = !string.IsNullOrEmpty(xDataItem.Identifier)
                ? xDataItem.Identifier
                : null;

            MDatasetSemaphores
                .Wait(dataset.Uri); // Ensure that only one thread can modify items in the same dataset at a time

            DataItem? oldDataItem = null;
            if (null != identifier)
                oldDataItem = dbContext.DataItems
                    .Include(d => d.Dataset)
                    .Include(d => d.Concept)
                    .AsSplitQuery()
                    .Where(d => dataset.Id == d.Dataset.Id && d.Identifier == identifier)
                    .AsEnumerable()
                    .SingleOrDefault(d => d.ConceptUri == xDataItem.ConceptUri);
            else
                identifier = concept.DefaultIdentifier != null
                    ? xDataItem.Values[concept.DefaultIdentifier.Name].ToString()!
                    : MDataContentsManagerCommon
                        .GetNextIntegerId(MDataContentsManagerCommon.GetUsedIntegerIds(dataset)).ToString();

            DataItem newDataItem = new DataItem
            {
                Identifier = identifier,
                Values = MapIncomingValues(xDataItem.Values, concept),
                Dataset = dataset,
                Concept = concept
            };

            if (null == oldDataItem)
                dbContext.DataItems.Add(newDataItem);
            else
            {
                newDataItem.Id = oldDataItem.Id; // Preserve unique identifier
                dbContext.Entry(oldDataItem).CurrentValues.SetValues(newDataItem);
                dbContext.Entry(oldDataItem).State = EntityState.Modified;
            }

            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw;
        } finally {
            MDatasetSemaphores.Release(xDataItem.DataSetUri);
        }
    }

    private Dictionary<string, object> MapIncomingValues(Dictionary<string, object> values, Concept concept)
    {
        Dictionary<string, object> result = new();
        foreach(Property prop in concept.Properties) {
            string targetName = prop.Name.ToCamelCase();
            string sourceName = targetName + (prop is Reference ? prop.Multiple ? "Ids" : "Id" : "");
            if (values.ContainsKey(sourceName)) {
                if (prop is not Reference) // no mapping needed for primitive types 
                    result[targetName] = values[sourceName];
                else if (!prop.Multiple) // single references have to have # added
                    result[targetName] = "#" + (values[sourceName] is JsonElement { ValueKind: JsonValueKind.String } je
                            ? je.GetString()
                            : throw new ArgumentException(
                                $"Value for reference property {prop.Name} should be string"));
                else  // multiple references need to have # added to all of them
                    result[targetName] = ((JsonElement)values[sourceName]).EnumerateArray()
                    .Select(p => "#" + p).ToList();
            } else if (prop.Required)
                throw new ArgumentException($"Missing value for property {prop.Name}");
        }
        return result;
    }
    
    private Dictionary<string, object> MapOutgoingValues(Dictionary<string, object> values, Concept concept)
    {
        Dictionary<string, object> result = new();
        foreach(Property prop in concept.Properties) {
            string sourceName = prop.Name.ToCamelCase();
            string targetName = sourceName + (prop is Reference ? prop.Multiple ? "Ids" : "Id" : "");
            if (values.ContainsKey(sourceName)) {
                if (prop is not Reference) // no mapping needed for primitive types 
                    result[targetName] = values[sourceName];
                else if (!prop.Multiple) // single references need to have # removed
                    result[targetName] = values[sourceName] is JsonElement { ValueKind: JsonValueKind.String } js
                          && '#' == js.GetString()![0]
                            ? js.GetString()![1..]
                            : throw new Exception(
                                $"Critical error: value for reference property {prop.Name} should be string and should start with '#'");
                else {
                    List<string> mappedReferences = [];
                    foreach (var reference in values[sourceName] is JsonElement { ValueKind: JsonValueKind.Array } je
                                 ? je.EnumerateArray()
                                 : throw new Exception($"Critical error: value for multiple reference property {prop.Name} should be array")) {
                        if ('#' == reference.GetString()![0])
                            mappedReferences.Add(reference.GetString()![1..]);
                        else throw new Exception($"Critical error: value for multiple reference property {prop.Name} should start with '#'");
                    }
                    result[targetName] = mappedReferences;
                }
            } else if (prop.Required)
                throw new Exception($"Critical error: missing value for property {prop.Name}");
        }
        return result;
    }

    public void DeleteDataItem(string identifier, string datasetUri, string? conceptUri, string userId)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        try
        {
            Dataset dataset = dbContext.Datasets
                                  .Include(d => d.Schema)
                                  .Include(d => d.Schema!.UsedNamespaces)
                                  .Include(d => d.Schema!.Concepts)
                                  .ThenInclude(c => c.Namespace)
                                  .Include(d => d.Schema!.Concepts)
                                    .ThenInclude(c => c.Properties)
                                  .Include(d => d.Schema!.Concepts)
                                    .ThenInclude(c => ((Reference) c.Properties).Type)
                                  .AsSplitQuery()
                                  .FirstOrDefault(d => d.Uri == datasetUri)
                              ?? throw new ArgumentException("Dataset not found");
            
            // ===> Authorisation
            CheckDataModificationAuthorisation(dataset, userId);
            
            if (null == dataset.Schema)
                throw new ArgumentException("Items can be retrieved only from datasets with schemas");
            string targetConceptUri = conceptUri ?? dataset.Schema!.MainConcept!.Uri
                ?? throw new ArgumentException(
                    "Concept URI has to be provided if the dataset schema does not define main concept");

            MDatasetSemaphores.Wait(dataset.Uri); // Ensure that only one thread can modify items in the same dataset at a time

            DataItem dataItem = dbContext.DataItems
                                    .Include(d => d.Dataset)
                                    .Include(d => d.Concept)
                                    .AsSplitQuery()
                                    .Where(d => d.Identifier == identifier && datasetUri == d.Dataset.Uri)
                                    .AsEnumerable()
                                    .SingleOrDefault(d => d.ConceptUri == targetConceptUri)
                                ?? throw new ArgumentException("DataItem not found");
            
            // Check that the data item is not referenced from another data item
            string refValue = "#" + identifier;
            foreach (Concept c in dataset.Schema.Concepts) {
                List<DataItem>? conceptItems = null;
                foreach (Reference r in c.Properties.OfType<Reference>()
                             .Where(r => r.Type.Uri == dataItem.ConceptUri)) {
                    conceptItems ??= dbContext.DataItems
                        .Where(d => d.Dataset.Uri == datasetUri)
                        .AsEnumerable()
                        .Where(d => d.ConceptUri == c.Uri)
                        .ToList();
                    string refValueKey = r.Name.ToCamelCase();
                    // Check if the data item is used in any of the data items of the current concept
                    if (!r.Multiple) {
                        if (conceptItems
                            .Any(d => d.Values.ContainsKey(refValueKey)
                                      && d.Values[refValueKey].ToString() == refValue))
                            throw new ArgumentException("DataItem is used by other data item(s)");
                    } else {
                        if (conceptItems
                            .Any(d => d.Values.ContainsKey(refValueKey)
                                      && ((JsonElement)d.Values[refValueKey]).EnumerateArray()
                                          .Any(v => v.ToString() == refValue)))
                            throw new ArgumentException("DataItem is used by other data item(s)");
                    }
                }
            }
            
            dbContext.Entry(dataItem).State = EntityState.Deleted;
            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw;
        } finally {
            MDatasetSemaphores.Release(datasetUri);
        }
    }
    
    public int CheckDataItem(XDataItem xDataItem, string userId)
    {
        Dataset dataset = dbContext.Datasets
                              .Include(d => d.Schema)
                              .Include(d => d.Schema!.UsedNamespaces)
                              .Include(d => d.Schema!.Concepts)
                              .ThenInclude(c => c.Namespace)
                              .Include(d => d.Schema!.Concepts)
                              .ThenInclude(c => c.Properties)
                              .AsSplitQuery()
                              .FirstOrDefault(d => d.Uri == xDataItem.DataSetUri)
                          ?? throw new ArgumentException("Dataset not found");
        
        // ===> Authorisation
        CheckDataRetrievalAuthorisation(dataset, userId);
        
        if (null == dataset.Schema)
            throw new ArgumentException("Items can be checked only for datasets with schemas");
        string targetConceptUri = xDataItem.ConceptUri ?? dataset.Schema!.MainConcept!.Uri
            ?? throw new ArgumentException(
                "Concept URI has to be provided if the dataset schema does not define main concept");
        Concept concept = dataset.Schema.Concepts.FirstOrDefault(c => c.Uri == targetConceptUri) ??
                          throw new ArgumentException("Concept not found in the dataset schema");
        string json = JsonSerializer.Serialize(xDataItem.Values);
        JsonNode instance = JsonNode.Parse(json)!;
        JsonSchema conceptSchema = JsonSchemaBuilder.BuildSchema(concept, true);
        EvaluationOptions options = new EvaluationOptions { OutputFormat = OutputFormat.List };
        EvaluationResults result = conceptSchema.Evaluate(instance, options);
        if (!result.IsValid) return 1;
        List<DataItem> allDataItems = dbContext.DataItems
            .Include(d => d.Dataset)
            .Include(d => d.Concept)
            .AsSplitQuery()
            .Where(d => xDataItem.DataSetUri == d.Dataset.Uri)
            .ToList();
        List<DataItem> dataItems = allDataItems
            .Where(d => d.ConceptUri == targetConceptUri)
            .ToList();
        string? identifier = !string.IsNullOrEmpty(xDataItem.Identifier)
            ? xDataItem.Identifier
            : null;
        // Check that all "Unique" properties are indeed unique (not found in any other existing Data Item,
        // except for the updated Data Item in case of update) 
        foreach (Property prp in concept.Properties.Where(p => p.Unique)) {
            if (!xDataItem.Values.ContainsKey(prp.Name.ToCamelCase())) {
                if (prp.Required)
                    return 1; // Unique property is missing, which is not allowed for required properties
                else
                    continue; // Unique property is missing, but it is not required, so we can skip the uniqueness check
            }
            string? value = xDataItem.Values[prp.Name.ToCamelCase()].ToString();
            if (null != value && prp is Reference) // for reference properties check the value with '#' prefix, as it is the format used in DataItem.Values
                value = "#" + value;
            if (dataItems.Any(d => d.Values.ContainsKey(prp.Name.ToCamelCase())
                                   && d.Values[prp.Name.ToCamelCase()].ToString() == value
                                   && (null == identifier || d.Identifier != identifier)))
                return 1;
        }

        // Check that Reference properties indeed refer to existing Data Items (in the current Dataset)
        foreach (Reference r in concept.Properties.OfType<Reference>()) { // for each reference in the concept
            if (!r.Multiple) {
                if (xDataItem.Values.TryGetValue(r.Name.ToCamelCase() + "Id", out var value) // such reference exists in the data item
                    && !allDataItems.Any(d => d.ConceptUri == r.Type.Uri && d.Identifier == value.ToString())) // such reference does not exist in the DB
                    return 1; // then error
            } else if (xDataItem.Values.TryGetValue(r.Name.ToCamelCase() + "Ids", out var values)) { // such references exist in the data item
                foreach (String id in values is JsonElement { ValueKind: JsonValueKind.Array } je // such references does not exist in the DB
                             ? je.EnumerateArray().Select(e => e.GetString()!)
                             : throw new ArgumentException($"Value for multiple reference property {r.Name} should be a JSON array")) {
                    if (!allDataItems.Any(d => d.ConceptUri == r.Type.Uri && d.Identifier == id))
                        return 1; // then error
                }
            }
        }
        return 0;
    }

    public int CheckExistingDataItem(string identifier, string datasetUri, string? conceptUri, string userId)
    {
        return 0; // throw new NotImplementedException();
    }

    public void SubmitSourceDatasetContents(string datasetUri, string userId)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        string? activeDatasetUri = null;
        try {
            Dataset sourceDataset = dbContext.Datasets
                                        .Include(d => d.Target)
                                        .AsSplitQuery()
                                        .FirstOrDefault(d => d.Uri == datasetUri)
                                    ?? throw new ArgumentException("Source dataset not found");
            activeDatasetUri = (sourceDataset.Target ?? throw new ArgumentException("Target dataset not defined")).Uri;
            
            if (activeDatasetUri == datasetUri)
                throw new ArgumentException("Source and target datasets are the same");
            
            // ===> Authorisation
            CheckDataRetrievalAuthorisation(sourceDataset, userId);
            CheckDataModificationAuthorisation(sourceDataset.Target, userId);

            MDatasetSemaphores
                .Wait(datasetUri); // lock the source dataset to prevent concurrent submissions from the same source dataset
            MDatasetSemaphores
                .Wait(activeDatasetUri); // lock the target dataset to prevent concurrent modifications during submission

            sourceDataset = dbContext.Datasets
                .Include(d => d.Schema)
                .Include(d => d.Schema!.UsedNamespaces)
                .Include(d => d.Schema!.Concepts)
                .ThenInclude(c => c.Properties)
                .Include(d => d.Schema!.Concepts)
                .ThenInclude(c => c.Namespace)
                .Include(d => d.Target)
                .Include(d => d.Target!.Items)
                .ThenInclude(i => i.Source)
                .Include(d => d.Target!.Items)
                .ThenInclude(i => i.Concept)
                .Include(d => d.Items)
                .ThenInclude(i => i.Concept)
                .AsSplitQuery()
                .FirstOrDefault(d => d.Uri == datasetUri)!;
            Dataset activeDataset = sourceDataset.Target!;

            // Get data items in the active dataset that were previously submitted from the current source dataset
            List<DataItem> previousDataItems =
                activeDataset.Items.Where(i => i.Source?.Uri == sourceDataset.Uri).ToList();
            // Delete those data items that are not present in the source dataset anymore
            foreach (DataItem deprecatedItem in previousDataItems
                         .Where(activeItem => !sourceDataset
                             .Items
                             .Any(sourceItem =>
                                 GetTargetIdentifier(sourceItem.Identifier, sourceDataset) == activeItem.Identifier
                                 && sourceItem.ConceptUri == activeItem.ConceptUri)))
                dbContext.Entry(deprecatedItem).State = EntityState.Deleted;
            // Add or update data items from the source dataset to the active dataset
            foreach (DataItem sourceItem in sourceDataset.Items) {
                DataItem? existingDataItem = activeDataset.Items
                    .FirstOrDefault(activeItem =>
                        activeItem.Identifier == GetTargetIdentifier(sourceItem.Identifier, sourceDataset)
                        && activeItem.ConceptUri == sourceItem.ConceptUri);
                if (existingDataItem == null) {
                    DataItem newDataItem = new DataItem {
                        Identifier = GetTargetIdentifier(sourceItem.Identifier, sourceDataset),
                        Values = UpdateReferences(sourceItem.Values, sourceDataset, sourceItem.Concept),
                        Dataset = activeDataset,
                        Concept = sourceItem.Concept,
                        Source = sourceDataset
                    };
                    dbContext.DataItems.Add(newDataItem);
                } else
                    existingDataItem.Values = UpdateReferences(sourceItem.Values, sourceDataset, sourceItem.Concept);
            }

            dbContext.SaveChanges();
            transaction.Commit();
        } catch (Exception) {
            dbContext.ChangeTracker.Clear(); transaction.Rollback(); throw;
        } finally {
            MDatasetSemaphores.Release(datasetUri);
            if (activeDatasetUri != null)
                MDatasetSemaphores.Release(activeDatasetUri);
        }
    }

    private string GetTargetIdentifier(string identifier, Dataset sourceDataset)
    {
        return sourceDataset.Uri + "/" + identifier;
    }

    private Dictionary<string,object> UpdateReferences(Dictionary<string,object> values,
        Dataset sourceDataset, Concept concept)
    {
        foreach (Reference reference in concept.Properties.OfType<Reference>()) {
            string sourceRefIdKey = reference.Name.ToCamelCase();
            if (values.ContainsKey(sourceRefIdKey)) {
                string sourceRefId = values[sourceRefIdKey].ToString()![1..];
                values[sourceRefIdKey] = "#" + GetTargetIdentifier(sourceRefId, sourceDataset);
            }
        }
        return values;
    }
}