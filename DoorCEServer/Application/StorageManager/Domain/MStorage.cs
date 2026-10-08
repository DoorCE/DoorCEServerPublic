using AutoMapper;
using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.BinaryContents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Domain;
using DoorCEServer.Application.StorageManager.Dtos;
using DoorCEServer.Application.StorageManager.Interfaces;
using DoorCEServer.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoorCEServer.Application.StorageManager.Domain;

public class MStorage(IMapper mapper, ApplicationDbContext context, MAgentsCommon agents) : StorageAPI
{
    private static readonly int MaxFileSizeInMb = 100;
    private static readonly int MaxIconSizeInMb = 2; // 2 MB
    private static readonly string[] AllowedMimeTypes =
    [
        "image/jpeg",
        "image/png"
    ];
    
    public int UpsertFile(XFileData metadata, IFormFile? file, string userId)
    {
        // ===> Authorisation
        UserAccount user = agents.GetUserAccount(userId)!;
        agents.CheckStorageAuthorisation(user);
        
        // check if file is null or empty
        if (file == null || file.Length == 0)
            throw new ArgumentException("File cannot be null or empty", nameof(file));
        
        // check if file is too big
        long maxFileSize = MaxFileSizeInMb * 1024 * 1024;
        if (file.Length > maxFileSize)
            throw new ArgumentException($"File size exceeds the maximum allowed size of {MaxFileSizeInMb} MB", nameof(file));
        
        // create a new file entity
        byte[] fileBytes;
        using (var memoryStream = new MemoryStream())
        {
            file.CopyTo(memoryStream);
            fileBytes = memoryStream.ToArray();
        }
        
        var newVersion = new DataFile
        {
            Name = metadata.FileName,
            ContentType = file.ContentType,
            Content = fileBytes,
            ModificationDate = DateTime.UtcNow.ToUniversalTime()
        };
        
        // *** UPSERT ***
        
        using var transaction = context.Database.BeginTransaction();
        try { 
           
           // Old version does not exist? - Add the new file
           if (null == metadata.FileId) {
               context.DataFiles.Add(newVersion);
               context.SaveChanges();
               transaction.Commit();
               return newVersion.Id;
           }
           
           // Otherwise - Update the old file with new data
           
           // Get the old version if it exists in the database
           DataFile? oldVersion = context.DataFiles.SingleOrDefault(f => f.Id == metadata.FileId);
           if (null == oldVersion)
               throw new ArgumentException($"File with ID {metadata.FileId} does not exist");
           newVersion.Id = (int)metadata.FileId; // make sure to keep the same ID
           context.Entry(oldVersion).CurrentValues.SetValues(newVersion);
           context.Entry(oldVersion).State = EntityState.Modified;
           context.SaveChanges();
           transaction.Commit();
           return oldVersion.Id;
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback(); throw; }  
    }

    public FileContentResult GetFile(int identifier, string? userId)
    {
        DataFile? file = context.DataFiles
            .SingleOrDefault(f => f.Id == identifier);

        Distribution? distribution = null != file ? GetFileDistribution(identifier) : null;
        
        // ===> Authorisation
        if (null == distribution || !agents.CheckVisibilityAndSetRoles(distribution, userId))
            throw new NotFoundOrVisibleException($"File [{identifier}] not found or not visible to the user");
        
        return new FileContentResult(file!.Content, file.ContentType)
        {
            FileDownloadName = file.Name,
            LastModified = file.ModificationDate
        };
    }

    public void DeleteFile(int identifier, string userId)
    {
        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try {
            DataFile? file = context.DataFiles
                .SingleOrDefault(f => f.Id == identifier);
            
            Distribution? distribution = null != file ? GetFileDistribution(identifier) : null;
            
            // ===> Authorisation
            if (null == distribution || !agents.CheckVisibilityAndSetRoles(distribution, userId))
                throw new ArgumentException($"File [{identifier}] not found or not visible to the user");
            if (!distribution.Dataset.HasDistributionRole())
                throw new UnauthorizedAccessException("This user has no privilege to delete this file");
            
            context.Entry(file!).State = EntityState.Deleted;
            context.SaveChanges();
            transaction.Commit();
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }
    
    public string UpsertIcon(XIconData metadata, IFormFile? file, string userId)
    {
        // ===> Authorisation
        UserAccount user = agents.GetUserAccount(userId)!;
        agents.CheckStorageAuthorisation(user, true);
        
        // check if file is null or empty
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is null or empty");
        
        // check if file is too big
        long maxFileSize = MaxIconSizeInMb * 1024 * 1024;
        if (file.Length > maxFileSize)
            throw new ArgumentException($"File is too large. Maximum size is {MaxIconSizeInMb} MB.");
        
        // check if file is of allowed type
        if (!AllowedMimeTypes.Contains(file.ContentType))
            throw new ArgumentException(
                $"File type {file.ContentType} is not allowed. Allowed types are: {string.Join(", ", AllowedMimeTypes)}");
        
        // create a new file entity
        byte[] fileBytes;
        using (var memoryStream = new MemoryStream())
        {
            file.CopyTo(memoryStream);
            fileBytes = memoryStream.ToArray();
        }
        
        var newVersion = new Icon
        {
            Uri = metadata.IconUri,
            Name = metadata.IconName,
            ContentType = file.ContentType,
            Content = fileBytes,
            ModificationDate = DateTime.UtcNow.ToUniversalTime()
        };
        
        // *** UPSERT ***
        
        using var transaction = context.Database.BeginTransaction();
        try { 
            // Get the old version if it exists in the database
           Icon? oldVersion = context.Icons
                .SingleOrDefault(i => i.Uri == newVersion.Uri);
           
           // Old version does not exist? - Add the new icon
           if (null == oldVersion) {
               context.Icons.Add(newVersion);
               context.SaveChanges();
               transaction.Commit();
               return newVersion.Uri;
           }
           
           // Otherwise - Update the old icon with new data
           newVersion.Id = oldVersion.Id; // make sure to keep the same ID
           context.Entry(oldVersion).CurrentValues.SetValues(newVersion);
           context.Entry(oldVersion).State = EntityState.Modified;
           
           context.SaveChanges();
           transaction.Commit();
           return oldVersion.Uri;
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback(); throw; }  
    }

    public IEnumerable<XIconData> GetIconList(string? query)
    {
        query = query?.ToLower();
        
        ICollection<Icon> icons = context.Icons
            .Where(i => string.IsNullOrEmpty(query) 
                        || query.Length < 3 
                        || i.Name.ToLower().Contains(query))
            .ToList();
        
        return mapper.Map<ICollection<Icon>, ICollection<XIconData>>(icons);
    }

    public FileContentResult GetIcon(string identifier)
    {
        Icon? icon = context.Icons
            .SingleOrDefault(i => i.Uri == identifier);
        if (null == icon)
            throw new NotFoundOrVisibleException($"Icon with identifier '{identifier}' not found");
        
        return new FileContentResult(icon.Content, icon.ContentType)
        {
            FileDownloadName = icon.Name,
            LastModified = icon.ModificationDate
        };
    }

    public void DeleteIcon(string identifier, string userId)
    {
        // ===> Authorisation
        UserAccount user = agents.GetUserAccount(userId)!;
        agents.CheckStorageAuthorisation(user, true);
        
        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try
        {
            if (context.OwnableResources.Any(or => or.IconUri == identifier))
                throw new InvalidOperationException("This icon is used by one or more resources and cannot be deleted");
            
            Icon? icon = context.Icons
                .SingleOrDefault(i => i.Uri == identifier);
            
            if (null == icon) throw new ArgumentException("Icon not found", nameof(identifier));
            
            context.Entry(icon).State = EntityState.Deleted;
            context.SaveChanges();
            transaction.Commit();
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }
    
    // ==== Private methods =============================================================================
    
    private Distribution? GetFileDistribution(int identifier)
    {
        return context.Distributions
            .Include(d => d.Dataset)
            .Include(d => d.DataService)
            .SingleOrDefault(d => d.FileId == identifier);
    }
}