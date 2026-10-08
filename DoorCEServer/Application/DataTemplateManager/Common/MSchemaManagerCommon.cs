using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;
using Microsoft.EntityFrameworkCore;

namespace DoorCEServer.Application.DataTemplateManager.Common;

public class MSchemaManagerCommon(ApplicationDbContext dbContext)
{
    public ICollection<Concept> PrepareConceptUpdate(ICollection<Concept> requestedVersions,
        ICollection<Concept> existingVersions)
    {
        foreach (Concept concept in existingVersions)
            if (requestedVersions.All(c => c.Name != concept.Name))
                dbContext.Entry(concept).State = EntityState.Deleted;
        
        List<Concept> updateConcepts = [];
        foreach (Concept concept in requestedVersions) {
            Concept? existingC = existingVersions
                .SingleOrDefault(c => c.Name == concept.Name);
            updateConcepts.Add(
                null != existingC ?
                    UpdateConcept(concept, existingC, existingVersions)
                    : concept);
        }

        return updateConcepts;
    }
    
    private Concept UpdateConcept(Concept requestedVersion, Concept existingVersion,
        ICollection<Concept> existingConcepts)
    {
        requestedVersion.Id = existingVersion.Id; // make sure to preserve the unique ID
        dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
        dbContext.Entry(existingVersion).State = EntityState.Modified;
        existingVersion.Properties = PreparePropertyUpdate(requestedVersion.Properties, existingVersion.Properties,
            existingConcepts);
        existingVersion.Namespace = requestedVersion.Namespace;
        return existingVersion;
    }
    
    private ICollection<Property> PreparePropertyUpdate(ICollection<Property> requestedVersions,
        ICollection<Property> existingVersions, ICollection<Concept> existingConcepts)
    {
        foreach (Property property in existingVersions)
            if (requestedVersions.All(p => p.Name != property.Name))
                dbContext.Entry(property).State = EntityState.Deleted;
        
        List<Property> updateProperties = [];
        foreach (Property property in requestedVersions) {
            Property? existingP = existingVersions
                .SingleOrDefault(p => p.Name == property.Name);
            updateProperties.Add(
                null != existingP ?
                    UpdateProperty(property, existingP, existingConcepts)
                    : property);
        }

        return updateProperties;
    }

    private Property UpdateProperty(Property requestedVersion, Property existingVersion,
        ICollection<Concept> existingConcepts)
    {
        bool changeOfType = requestedVersion.GetType() != existingVersion.GetType();
        if (requestedVersion is Reference requestedRef) {
            Concept? typeConcept = existingConcepts
                .SingleOrDefault(c => c.Name == requestedRef.Type.Name);
            if (null != typeConcept)
                requestedRef.Type = typeConcept;
        }
        
        if (changeOfType) {
            dbContext.Entry(existingVersion).State = EntityState.Deleted;
            return requestedVersion;
        }
        requestedVersion.Id = existingVersion.Id; // make sure to preserve the unique ID
        dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
        dbContext.Entry(existingVersion).State = EntityState.Modified;

        if (existingVersion is Reference existingRef)
            existingRef.Type = ((Reference)requestedVersion).Type;
        existingVersion.Namespace = requestedVersion.Namespace;
        return existingVersion;
    }
    
    public ICollection<Namespace> PrepareNamespaceUpdate(ICollection<Namespace> requestedVersions,
        ICollection<Namespace> existingVersions)
    {
        foreach (Namespace nspace in existingVersions.Where(n => n is CustomNamespace))
            if (requestedVersions.All(c => c.Prefix != nspace.Prefix))
                dbContext.Entry(nspace).State = EntityState.Deleted;
        
        List<Namespace> updateNamespaces = existingVersions.Where(n => n is not CustomNamespace).ToList();
        foreach (Namespace nspace in requestedVersions.Where(n => n is CustomNamespace)) {
            Namespace? existingN = existingVersions
                .SingleOrDefault(c => c.Prefix == nspace.Prefix);
            updateNamespaces.Add(
                null != existingN ?
                    UpdateNamespace(nspace, existingN)
                    : nspace);
        }

        return updateNamespaces;
    }
    
    private Namespace UpdateNamespace(Namespace requestedVersion, Namespace existingVersion)
    {
        requestedVersion.Id = existingVersion.Id; // make sure to preserve the unique ID
        dbContext.Entry(existingVersion).CurrentValues.SetValues(requestedVersion);
        dbContext.Entry(existingVersion).State = EntityState.Modified;
        return existingVersion;
    }
}