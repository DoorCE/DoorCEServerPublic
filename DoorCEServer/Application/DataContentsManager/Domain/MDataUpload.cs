using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEModel.Utils.Extensions;
using DoorCEServer.Application.CataloguingManager.Domain;
using DoorCEServer.Application.DataContentsManager.Common;
using DoorCEServer.Application.DataContentsManager.Domain.Converters;
using DoorCEServer.Application.DataContentsManager.Domain.Services;
using DoorCEServer.Application.DataContentsManager.Dtos;
using DoorCEServer.Application.DataContentsManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;
using Json.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using JsonSchemaBuilder = DoorCEServer.Application.DataContentsManager.Domain.Services.JsonSchemaBuilder;

namespace DoorCEServer.Application.DataContentsManager.Domain;

public class MDataUpload(ApplicationDbContext dbContext, MAgentsCommon agents, IHttpClientFactory httpClientFactory)
    : MDataManagement(agents), DataUploadAPI
{
    private static readonly int MaxFileSizeInMb = 100;
    
    // ==== "Convert" methods =======================================================================
    // These methods load, perform checks and convert files into UnifiedFile format
    // ==============================================================================================

    private UnifiedFile LoadAndConvertFile(XDataUploadRequest request, IFormFile? file = null) 
    {
        // check if file is null
        if (file == null && string.IsNullOrWhiteSpace(request.FileUrl))
            throw new ArgumentException("Provide either a file or a fileUrl.");
        
        // check if file is too big
        long maxFileSize = MaxFileSizeInMb * 1024L * 1024L;
        if (file?.Length > maxFileSize)
            throw new ArgumentException($"File size exceeds the maximum allowed size of {MaxFileSizeInMb} MB", nameof(file));
        
        // get file stream
        using Stream fileStream = GetFileStream(file, request.FileUrl);
        
        // get extension
        string ext = GetFileExtension(file, request.FileUrl, request.Extension);
        
        // get single sheet name
        string? singleSheetName = request.TableNameToConceptUriMap.Count == 1
            ? request.TableNameToConceptUriMap.Keys.First().ToLowerInvariant()
            : null;
        
        // get converter and convert
        var factory = new FormatConverterFactory();
        var converter = factory.GetConverter(ext);

        return converter.ReadFile(fileStream, singleSheetName);
    }
    
    private Stream GetFileStream(IFormFile? file = null, string? url = null)
    {
        if (file != null)
            return file.OpenReadStream();
        
        
            // Url validation
            if (!UrlSafety.ValidateFileUrlAsync(url!).GetAwaiter().GetResult())
                throw new ArgumentException("Unsafe or invalid URL.");

            // Load file from URL
            try
            {
                long maxFileSize = MaxFileSizeInMb * 1024L * 1024L;
                var httpClient = httpClientFactory.CreateClient("FileDownload");
        
                using HttpResponseMessage response = httpClient.GetAsync(url!, HttpCompletionOption.ResponseHeadersRead)
                    .GetAwaiter().GetResult(); // loads file directly to a stream
                
                if (!response.IsSuccessStatusCode) 
                    throw new Exception($"Failed to fetch file. Status: {response.StatusCode}");
                if (response.Content.Headers.ContentLength > maxFileSize)
                    throw new ArgumentException(
                        $"File size exceeds the maximum allowed size of {MaxFileSizeInMb} MB");
                
                var memoryStream = new MemoryStream();
                var buffer = new byte[81920];
                long totalRead = 0;
                
                using var sourceStream = response.Content.ReadAsStream();
                int bytesRead;
                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalRead += bytesRead;
                    if (totalRead > maxFileSize)
                    {
                        memoryStream.Dispose();
                        throw new ArgumentException(
                            $"File size exceeds the maximum allowed size of {MaxFileSizeInMb} MB");
                    }
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException) throw;
                throw new Exception($"Cannot download file: {ex.Message}");
            }
    }
    
    private static string GetFileExtension(IFormFile? file = null, string? url = null, string? extension = null)
    {
        // get extension
        string ext;
        string extFromFile = Path.GetExtension(file?.FileName ?? new Uri(url ?? string.Empty).AbsolutePath);
        if (extFromFile.Length > 0)
        {
            ext = extFromFile.ToLowerInvariant();
            if (ext.StartsWith(".")) ext = ext[1..];
        } 
        else if (null != extension)
        {
            ext = extension.ToLowerInvariant();
        }
        else
        {
            throw new ArgumentException("File extension not specified");
        }

        return ext;
    }
    // ==== End of "Convert" methods =================================================================
    
    // ==== "Process" methods ========================================================================
    // These methods process the UnifiedFile:
    // - assign concepts to respective sheets,
    // - filter sheets so the UnifiedFile includes only those requested by the user
    // - set reference/unique properties and id field names to sheets
    // - prepare values in reference properties
    // - set dataset to file
    // ===============================================================================================
    
    private UnifiedFile ProcessFile(XDataUploadRequest request, List<Concept> concepts, Dataset dataset, UnifiedFile file)
    {
        var tableNameToConceptMap = request.TableNameToConceptUriMap.ToDictionary(kv 
            => kv.Key.ToLowerInvariant(), kv => concepts.First(c => c.Uri == kv.Value));
        
        var processedFile = FilterSheetsAndAssignConcepts(file, tableNameToConceptMap);
        var conceptReferenceMap = GetPropertiesWithReferences(concepts);
        var conceptUniqueMap = GetUniqueProperties(concepts);
        foreach (var sheet in processedFile.Sheets)
        {
            sheet.ReferenceProperties = conceptReferenceMap
                .TryGetValue(sheet.Concept!.Uri!, out var props) ? props : new List<Property>();
            
            // cast reference values to string to allow validation
            foreach (var row in sheet.Rows)
            {
                foreach (var referenceProp in sheet.ReferenceProperties)
                {
                    if (row.TryGetValue(referenceProp.Name, out var refValue) &&
                        !string.IsNullOrEmpty(refValue.ToString()))
                    {
                        row[referenceProp.Name] = refValue.ToString()!;
                    }
                }
            }
            
            sheet.IdFieldName = GetIdFieldName(request, sheet.Concept, sheet);
            sheet.UniqueProperties = conceptUniqueMap
                .TryGetValue(sheet.Concept.Uri!, out var uProps) ? uProps : new List<Property>();
        }
        
        processedFile.Dataset = dataset;
        
        return processedFile;
    }

    private static string? GetIdFieldName(XDataUploadRequest request, Concept concept, UnifiedSheet sheet)
    {
        if (null != concept.DefaultIdentifier)
        {
            return concept.DefaultIdentifier.Name;
        }
        
        return request.TableNameToIdFieldNameMap
            .TryGetValue(sheet.Name, out var idField) && !string.IsNullOrEmpty(idField) ? idField : null;
    }
    
    private static UnifiedFile FilterSheetsAndAssignConcepts(UnifiedFile file,
        Dictionary<string, Concept> tableNameToConceptMap)
    {
        // check if number of sheets and number of concepts match
        if (file.Sheets.Count < tableNameToConceptMap.Count)
            throw new ArgumentException("Number of sheets in the file is less than number of mapped concepts.");
        
        // filter the sheets and replace appropriate sheet names
        var filteredSheets = file.Sheets.Where(s 
            => tableNameToConceptMap.ContainsKey(s.Name)).ToList();

        foreach (var sheet in filteredSheets)
        {
            sheet.Concept = tableNameToConceptMap[sheet.Name];
        }

        return new UnifiedFile(){Sheets = filteredSheets};
    }
    
    private Dictionary<string,List<Property>> GetPropertiesWithReferences(List<Concept> concepts)
    {
        var conceptPropertyMap = new Dictionary<string, List<Property>>();
        foreach (var concept in concepts)
        {
            var referenceProperties = concept.Properties
                .Where(p => p is Reference)
                .ToList();
            if (referenceProperties.Count > 0)
            {
                conceptPropertyMap[concept.Uri!] = referenceProperties;
            }
        }

        return conceptPropertyMap;
    }
    
    private Dictionary<string, List<Property>> GetUniqueProperties(List<Concept> concepts)
    {
        var conceptPropertyMap = new Dictionary<string, List<Property>>();
        foreach (var concept in concepts)
        {
            var uniqueProperties = concept.Properties
                .Where(p => p.Unique)
                .ToList();
            if (uniqueProperties.Count > 0)
            {
                conceptPropertyMap[concept.Uri!] = uniqueProperties;
            }
        }

        return conceptPropertyMap;
    }
    // ==== End of "Process" methods =================================================================
    
    // ==== "Get" methods ================================================================================
    // These methods retrieve objects from the database needed for processing and validation
    // ===============================================================================================
    private Dataset GetDatasetByUri(string datasetUri)
    {
        var dataset = dbContext.Datasets
            .Include(d => d.Schema)
            .Include(d => d.Schema!.Concepts)
                .ThenInclude(c => c.Properties)
            .Include(d => d.Schema!.Concepts)
                .ThenInclude(c => c.Namespace)
            .Include(d => d.Schema!.UsedNamespaces)
            .Include(d => d.Items)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == datasetUri);
        
        if (dataset == null)
            throw new ArgumentException($"Dataset {datasetUri} not found.");

        return dataset;
    }
    
    private DataSchema GetSchemaFromDataset(Dataset dataset)
    {
        if (dataset.Schema == null)
            throw new ArgumentException($"Dataset {dataset.Uri} does not have a schema assigned.");

        return dataset.Schema;
    }

    private List<Concept> GetConcepts(DataSchema schema, List<string> conceptUris)
    {
        var normalized = conceptUris.Select(n => n.ToLowerInvariant()).ToList();

        var concepts = schema.Concepts
            .Where(c => c.Uri != null && normalized.Contains(c.Uri)) //TODO - when uri is null?
            .ToList();

        if (concepts.Count != normalized.Count)
            throw new ArgumentException("Some concepts are not found in the dataset schema");

        foreach (var c in concepts)
        {
            c.Properties = c.Properties.Select(p =>
            {
                p.Name = p.Name.ToLowerInvariant();
                return p;
            }).ToList();
        }

        return concepts;
    }
    // ==== End of "Get" methods ========================================================================
    
    // ==== "Data Item" methods ==========================================================================
    // These methods handle operations related to DataItems, e.g. creation and upsertion to the database
    // ===================================================================================================
    private List<DataItem> ConvertFileToDataItems(UnifiedFile file)
    {
        var allItems = new List<DataItem>();
        
        var intIdsInDataset = GetUsedIntegerIds(file);

        foreach (var sheet in file.Sheets)
        {
            var allowedKeys = new HashSet<string>(sheet.Concept!.Properties.Select(p => p.Name));

            var firstMissingNumber = 1;
            foreach (var row in sheet.Rows)
            {
                // get identifier
                object identifierValue;
                if (!string.IsNullOrEmpty(sheet.IdFieldName) &&
                    row.TryGetValue(sheet.IdFieldName, out var idVal) &&
                    !string.IsNullOrEmpty(idVal.ToString()))
                {
                    identifierValue = idVal;
                    
                } else {
                    // if sheet does not have id field name or id value is missing - assign new integer id
                    // Find smallest non-negative missing number
                    firstMissingNumber = MDataContentsManagerCommon.GetNextIntegerId(intIdsInDataset, firstMissingNumber);

                    identifierValue = firstMissingNumber;
                    intIdsInDataset.Add(firstMissingNumber);
                    firstMissingNumber++;
                }
                
                // add "#" to reference values
                if (sheet.ReferenceProperties.Count > 0)
                {
                    foreach (var referenceProp in sheet.ReferenceProperties)
                    {
                        if (row.TryGetValue(referenceProp.Name, out var refValue) &&
                            !string.IsNullOrEmpty(refValue.ToString()) &&
                            refValue is string refString &&
                            !refString.StartsWith("#"))
                        {
                            row[referenceProp.Name] = $"#{refString}";
                        }
                    }
                }

                // TODO - preserving column names that are already in camel case (now they get converted to lower case)
                // filter values
                var values = row
                    .Where(kvp => allowedKeys.Contains(kvp.Key)).ToDictionary(kv => kv.Key.ToCamelCase(), kv => kv.Value);

                if (values.Count == 0)
                {
                    throw new FileValidationException(
                        "No values to be uploaded into the database found. The file properties do not match the schema.");
                }

                var dataItem = new DataItem
                {
                    Identifier = identifierValue.ToString()!,
                    Dataset = file.Dataset!,
                    Concept = sheet.Concept,
                    Values = values
                };

                allItems.Add(dataItem);
            }
        }

        return allItems;
    }
    
    private HashSet<int> GetUsedIntegerIds(UnifiedFile file)
    {
        // keep track of used integer IDs to assign new ones if needed
        var intIdsInDataset = MDataContentsManagerCommon.GetUsedIntegerIds(file.Dataset!);

        // collect integer IDs already present in the file
        foreach (var sheet in file.Sheets.Where(s => !string.IsNullOrEmpty(s.IdFieldName)))
        {
            var ids = sheet.Rows
                .Where(r => r.TryGetValue(sheet.IdFieldName!, out var idVal) &&
                            !string.IsNullOrEmpty(idVal.ToString()) &&
                            Int32.TryParse(idVal.ToString(), out _))
                .Select(r => Int32.Parse(r[sheet.IdFieldName!].ToString()!))
                .ToHashSet();
            intIdsInDataset.UnionWith(ids);
        }
        
        return intIdsInDataset;
    }
    
    private void UpsertDataItemList(List<DataItem> dataItems, Dataset dataset)
    {
        MDatasetSemaphores.Wait(dataset.Uri);
        
        using var transaction = dbContext.Database.BeginTransaction();
        try
        {
            foreach (var item in dataItems)
            {
                var oldItem = !string.IsNullOrWhiteSpace(item.Identifier)
                    ? dbContext.DataItems
                        .Include(d => d.Dataset)
                        .SingleOrDefault(d => d.Identifier == item.Identifier && d.Dataset.Uri == dataset.Uri)
                    : null;

                if (oldItem == null)
                {
                    dbContext.DataItems.Add(item);
                }
                else
                {
                    throw new FileValidationException
                        ("Update not supported: " +
                         "file contains items with identifiers that already exist in the dataset.");
                    
                    // TODO - implement update
                    // item.Id = oldItem.Id; // make sure to preserve the unique ID
                    // dbContext.Entry(oldItem).CurrentValues.SetValues(item);
                    // dbContext.Entry(oldItem).State = EntityState.Modified;
                }
            }

            dbContext.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            dbContext.ChangeTracker.Clear();
            transaction.Rollback();
            throw;
        }
        finally
        {
            MDatasetSemaphores.Release(dataset.Uri);
        }
    }
    // ==== End of "Data Item" methods ===================================================================
    
    // ==== "Validation" methods ==========================================================================
    // These methods handle file validation against JSON schemas built from concepts
    // ===================================================================================================
    
    private Dictionary<string, JsonSchema> BuildJsonSchemas(List<Concept> concepts)
    {
        // build a JSON schema for each concept
        var jsonSchemas = new Dictionary<string, JsonSchema>();
        
        foreach (var concept in concepts)
        {
            var jsonSchema = JsonSchemaBuilder.BuildSchema(concept);
            jsonSchemas.Add(concept.Uri!.ToLowerInvariant(), jsonSchema);
        }
        
        return jsonSchemas;
    }

    public XFileValidationResult ValidateFile(XDataUploadRequest request, IFormFile? file = null)
    {
        var convertedFile = LoadAndConvertFile(request, file);
        
        var conceptList = request.TableNameToConceptUriMap.Values.ToList();
        var dataset = GetDatasetByUri(request.DatasetUri);
        var schema = GetSchemaFromDataset(dataset);
        var concepts = GetConcepts(schema, conceptList);
        
        var unifiedFile = ProcessFile(request, concepts, dataset, convertedFile);
        
        var jsonSchemas = BuildJsonSchemas(concepts);
        
        var validator = new FileValidator(dbContext, unifiedFile, jsonSchemas);
        var result = validator.CheckFileValidity();

        return result;
    }

    public List<DataItem> TestValidateAndConvertFile(XDataUploadRequest request, 
        IFormFile? file = null)
    {
        var convertedFile = LoadAndConvertFile(request, file);
        
        var conceptList = request.TableNameToConceptUriMap.Values.ToList();
        var dataset = GetDatasetByUri(request.DatasetUri);
        var schema = GetSchemaFromDataset(dataset);
        var concepts = GetConcepts(schema, conceptList);
        
        var unifiedFile = ProcessFile(request, concepts, dataset, convertedFile);
        
        var jsonSchemas = BuildJsonSchemas(concepts);
        
        var converter = new FileValidator(dbContext, unifiedFile, jsonSchemas);
        var result = converter.CheckFileValidity();
        
        var conceptUriToIdFieldNameMap = request.TableNameToConceptUriMap
            .ToDictionary(kv => kv.Value.ToLowerInvariant(), 
                kv => request.TableNameToIdFieldNameMap[kv.Key].ToLowerInvariant());

        return !result.IsValid 
            ? throw new FileValidationException("File not valid!") 
            : ConvertFileToDataItems(unifiedFile);
    }
    // ==== End of "Validation" methods ===================================================================
    
    // ==== API methods ===================================================================================
    // These methods implement the DataUploadAPI interface to be used by the Web API controller
    // ===================================================================================================
    
    public void ClearDataItemsByDatasetUri(string datasetUri, string? userId)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        try
        {
            var itemsToDelete = dbContext.DataItems
                .Include(d => d.Dataset)
                .AsSplitQuery()
                .Where(d => d.Dataset.Uri == datasetUri)
                .ToList();
            
            if (itemsToDelete.Count == 0)
                return;
            
            // ===> Authorisation
            CheckDataModificationAuthorisation(itemsToDelete.First().Dataset, userId);

            dbContext.DataItems.RemoveRange(itemsToDelete);

            dbContext.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            dbContext.ChangeTracker.Clear();
            transaction.Rollback();
            throw;
        }
    }
    
    public Dictionary<string, int> GetDataItemCountsByConcept(string datasetUri)
    {
        var dataset = dbContext.Datasets
            .Include(d => d.Schema)
            .Include(d => d.Items)
                .ThenInclude(i => i.Concept)
                    .ThenInclude(c => c.Properties)
            .Include(d => d.Items)
                .ThenInclude(i => i.Concept)
                    .ThenInclude(c => c.Schema)
                        .ThenInclude(s => s!.UsedNamespaces)
            .Include(d => d.Items)
                .ThenInclude(i => i.Concept)
                    .ThenInclude(c => c.Namespace)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == datasetUri);
        
        if (dataset == null)
            throw new ArgumentException($"Dataset {datasetUri} not found.");
        if (dataset.Schema == null && dataset.Items.Count == 0)
            return new Dictionary<string, int>();
        
        var counts = dataset.Items
            .GroupBy(d => d.Concept.Uri)
            .Select(g => new { ConceptUri = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ConceptUri!, x => x.Count);

        return counts;
    }
    
    public XFileValidationResult UploadData (XDataUploadRequest request, string? userId, IFormFile? file=null)
    {
        // get dataset
        var dataset = GetDatasetByUri(request.DatasetUri);
        
        // ===> Authorisation
        //TODO - check if authorisation is correct
        CheckDataModificationAuthorisation(dataset, userId);
        
        // modify request maps to be case-insensitive
        request.TableNameToConceptUriMap = request.TableNameToConceptUriMap.ToDictionary(kv 
            => kv.Key.ToLowerInvariant(), kv => kv.Value.ToLowerInvariant());
        request.TableNameToIdFieldNameMap = request.TableNameToIdFieldNameMap.ToDictionary(kv 
            => kv.Key.ToLowerInvariant(), kv => kv.Value.ToLowerInvariant());
        
        // convert file
        var convertedFile = LoadAndConvertFile(request, file);
        
        // get schema, concepts
        var conceptList = request.TableNameToConceptUriMap.Values.ToList();
        var schema = GetSchemaFromDataset(dataset);
        var concepts = GetConcepts(schema, conceptList);
        
        // process file: assign concepts, filter sheets, set reference properties and ids to sheets, set dataset to file
        var unifiedFile = ProcessFile(request, concepts, dataset, convertedFile);
        
        // build JSON schemas
        var jsonSchemas = BuildJsonSchemas(concepts);
        
        // validate file with json schemas
        var converter = new FileValidator(dbContext, unifiedFile, jsonSchemas);
        var result = converter.CheckFileValidity();

        // if not valid - throw exception with errors
        if (!result.IsValid)
        {
            var jsonErrors = JsonConvert.SerializeObject(result.Errors);
            throw new FileValidationException(jsonErrors);
        }

        // if valid - convert to XDataItems and upsert to DB
        var dataItems = ConvertFileToDataItems(unifiedFile);
        UpsertDataItemList(dataItems, dataset);
        return result;
    }
}