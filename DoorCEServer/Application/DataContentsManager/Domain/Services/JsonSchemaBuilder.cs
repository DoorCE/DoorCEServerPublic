using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;
using Json.Schema;
using Newtonsoft.Json.Linq;
using Attribute = DoorCEModel.Infrastructure.DataModel.DataSchemas.Attribute;

namespace DoorCEServer.Application.DataContentsManager.Domain.Services;

public class JsonSchemaBuilder
{
    
    private static List<string> GetRequiredPropertiesNames(Concept concept, bool useIdForReferences)
    {
        return concept.Properties
            .Where(p => p.Required)
            .Select(p => p.Name.ToLowerInvariant() +
                         (p is Reference && useIdForReferences ? p.Multiple ? "Ids" : "Id" : ""))
            .ToList();
    }
    
    public static JsonSchema BuildSchema(Concept concept, bool useIdForReferences = false)
    {
        var schema = new JObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = concept.Name,
            ["description"] = concept.Description ?? "",
            ["type"] = "object", 
            ["properties"] = BuildProperties(concept.Properties, useIdForReferences),
            ["required"] = new JArray(GetRequiredPropertiesNames(concept, useIdForReferences)),
            ["additionalProperties"] = true,
        };

        return ToJsonSchema(schema);
    }

    private static JsonSchema ToJsonSchema(JObject obj)
    {
        var jsonString = obj.ToString();
        return JsonSchema.FromText(jsonString);
    }

    private static JObject BuildConcepts(IEnumerable<Concept> concepts, bool useIdForReferences)
    {
        var obj = new JObject();

        foreach (var concept in concepts)
        {
            obj[concept.Name] = new JObject
            {
                ["type"] = "object",
                ["description"] = concept.Description,
                ["properties"] = BuildProperties(concept.Properties, useIdForReferences),
                ["required"] = new JArray(GetRequiredPropertiesNames(concept, useIdForReferences)),
                ["additionalProperties"] = true
            };
        }

        return obj;
    }

    private static JObject BuildProperties(ICollection<Property> props, bool useIdForReferences)
    {
        var obj = new JObject();

        foreach (var prop in props)
        {
            if (prop.Multiple)
            {
                var j = new JObject();

                if (prop is Reference && useIdForReferences)
                {
                    obj[prop.Name.ToLowerInvariant() + "Ids"] = j;
                    j["type"] = "array";
                    j["items"] = new JObject(
                        new JProperty("type", "string")
                    );
                }
                else
                {
                    obj[prop.Name.ToLowerInvariant()] = j;
                    j["type"] = "array";
                    j["items"] = BuildPropertyValue(prop);
                }
            }
            else
            {
                obj[prop.Name.ToLowerInvariant() + (prop is Reference && useIdForReferences ? "Id" : "")]
                    = BuildPropertyValue(prop);
            }
        }

        return obj;
    }

    private static JObject BuildPropertyValue(Property prop)
    {
        var j = new JObject();
        
        if (prop is Attribute attribute)
        {
            j["type"] = attribute.Type.ToJsonSchemaType();
            if (attribute.Type is PrimitiveType.Date or PrimitiveType.Time or PrimitiveType.DateTime) {
                j["pattern"] = @"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})";
            }

            if (attribute.Type == PrimitiveType.Location) {
                j["pattern"] = @"^\(\s*-?\d+(\.\d+)?\s*,\s*-?\d+(\.\d+)?\s*\)$";
            }
            // if (attribute.Type == PrimitiveType.Location) {
            //     j["properties"] = new JObject {
            //         ["Item1"] = new JObject { ["type"] = "number" },
            //         ["Item2"] = new JObject { ["type"] = "number" }
            //     };
            //     j["required"] = new JArray{"Item1", "Item2"};
            //     j["additionalProperties"] = false;
            // }
        }
        else if (prop is Reference)
        {
            j["type"] = "string";
        }

        if (prop.Description != null)
            j["description"] = prop.Description;
        

        return j;
    }
}