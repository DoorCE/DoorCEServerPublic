using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.MockDtos;
using Microsoft.EntityFrameworkCore;

namespace DoorCEServer.Tests.CataloguingManager.DtoHelpers;

public class DatasetHelper(ApplicationDbContext context, DatasetManagementAPI datasetApi, TestCommon testCommon)
{
    private readonly XDatasetMock _xDatasetMock = new();
    
    public XDataset? GetFromDb(DatasetLabel datasetLabel)
    {
        XDataset xDataset = _xDatasetMock.Get(datasetLabel, VariantLabel.AfterUpsert);
        try {
            return datasetApi.GetDataset(xDataset.Uri!, "udas-admin");
        } catch (NotFoundOrVisibleException) { return null; }
    }

    public Dataset GetModel(DatasetLabel datasetLabel) 
    {
        XDataset xDataset = _xDatasetMock.Get(datasetLabel, VariantLabel.AfterUpsert);
        
        Dataset? dataset = context.Datasets
            .Include(d => d.Catalogue)
            .Include(d => d.ResponsiblePerson)
            .Include(d => d.ResponsibleOrganisation)
            .Include(d => d.Contacts)
            .Include(d => d.EditorshipsLink)
            .Include(d => d.EditorshipsLink.Editors)
            .Include(d => d.EditorshipsLink.EditorRoles)
            .Include(d => d.Series)
            .Include(d => d.Distributions)
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
            .Include(d => d.Schema)
            .ThenInclude(s => s!.Concepts)
            .ThenInclude(c => c.Properties)
            .Include(d => d.Schema)
            .ThenInclude(s => s!.UsedNamespaces)
            .Include(d => d.Schema)
            .ThenInclude(s => s!.Concepts)
            .ThenInclude(c => c.Namespace)
            .AsSplitQuery()
            .SingleOrDefault(d => d.Uri == xDataset.Uri);
        
        if (dataset == null)
            throw new NotFoundOrVisibleException($"Dataset {datasetLabel} not found");

        return dataset;
    }

    public void Upsert(DatasetLabel datasetLabel, VariantLabel variantLabel, ContactsLabel contactsLabel)
    {
        XDataset xDataset = _xDatasetMock.Get(datasetLabel, variantLabel);
        List<XContactData> xContacts = _xDatasetMock.GetContacts(datasetLabel, contactsLabel);
        
        datasetApi.UpsertDataset(xDataset, xContacts,"udas-admin");
    }

    public void Delete(DatasetLabel datasetLabel)
    {
        XDataset xDataset = _xDatasetMock.Get(datasetLabel, VariantLabel.AfterUpsert);
        datasetApi.DeleteDataset(xDataset.Uri!, "udas-admin");
    }
    
    public bool ValidateDelete(DatasetLabel datasetLabel, VariantLabel variantBeforeDelete)
    {
        XDataset datasetBeforeDelete = _xDatasetMock.Get(datasetLabel, variantBeforeDelete);
        
        // check if dataset was deleted
        if (!context.Datasets.Any(ds => ds.Uri == datasetBeforeDelete.Uri)) {
            // check if contacts were deleted
            return datasetBeforeDelete.ContactsUris
                .All(contactUri => !context.ContactDatas.Any(c => c.Uri == contactUri));
        }
        return false;
    }

    public bool Validate(XDataset xDataset, DatasetLabel datasetLabel, VariantLabel variantToCompare)
    {
        return testCommon.CheckDatasetEquality(xDataset, _xDatasetMock.Get(datasetLabel, variantToCompare));
    }
}