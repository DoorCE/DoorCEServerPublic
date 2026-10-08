using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Applications;
using DoorCEModel.Infrastructure.DataModel.BinaryContents;
using DoorCEModel.Infrastructure.DataModel.CodeContents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Infrastructure.DataModel.DataSchemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Attribute = DoorCEModel.Infrastructure.DataModel.DataSchemas.Attribute;

namespace DoorCEModel.Infrastructure;

public class ApplicationDbContext : DbContext
{
    private readonly bool _usePostgis;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : base(options)
    {
        bool.TryParse(configuration["UsePostgis"], out _usePostgis);
    }
    public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<DatasetSeries> DatasetSeries => Set<DatasetSeries>();
    public DbSet<Catalogue> Catalogues => Set<Catalogue>();
    public DbSet<CataloguedResource> CataloguedResources => Set<CataloguedResource>();
    public DbSet<OwnableResource> OwnableResources => Set<OwnableResource>();
    public DbSet<ResourceEditorshipsLink> ResourceEditorshipsLinks => Set<ResourceEditorshipsLink>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<ContactData> ContactDatas => Set<ContactData>();
    public DbSet<Distribution> Distributions => Set<Distribution>();
    // public DbSet<FileDistribution> FileDistributions => Set<FileDistribution>();
    // TODO - decide on the storage of FileDistributions
    public DbSet<DataService> DataServices => Set<DataService>();
    public DbSet<DataSchema> DataSchemas => Set<DataSchema>();
    public DbSet<Standard> Standards => Set<Standard>();
    public DbSet<SchemaSeries> SchemaSeries => Set<SchemaSeries>();
    public DbSet<NamespaceElement> NamespaceElements => Set<NamespaceElement>();
    public DbSet<Attribute> Attributes => Set<Attribute>();
    public DbSet<Reference> References => Set<Reference>();
    public DbSet<Concept> Concepts => Set<Concept>();
    public DbSet<CustomNamespace> CustomNamespaces => Set<CustomNamespace>();
    public DbSet<Namespace> Namespaces => Set<Namespace>();
    
    public DbSet<AppTemplate> AppTemplates => Set<AppTemplate>();
    public DbSet<AcquisitionApp> AcquisitionApps => Set<AcquisitionApp>();

    public DbSet<DataItem> DataItems => Set<DataItem>();
    public DbSet<Icon> Icons => Set<Icon>();
    public DbSet<DataFile> DataFiles => Set<DataFile>();
    
    public DbSet<CodePackage> CodePackages => Set<CodePackage>();
    public DbSet<CodeFile> CodeFiles => Set<CodeFile>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // This should be used in case configuration would need to be be split into separate classes
        // builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        if (_usePostgis)
        { 
            builder.HasPostgresExtension("postgis");
        }
        
        // Ignore computed properties
        builder.Entity<Agent>().Ignore(p => p.FullName);
        builder.Entity<Agent>().Ignore(p => p.UserRoles);
        builder.Entity<OwnableResource>().Ignore(o => o.EditorRoles);
        builder.Entity<OwnableResource>().Ignore(o => o.Editors);
        builder.Entity<SchemaSeries>().Ignore(o => o.EditorRoles);
        builder.Entity<SchemaSeries>().Ignore(o => o.Editors);
        builder.Entity<AcquisitionApp>().Ignore(o => o.EditorRoles);
        builder.Entity<AcquisitionApp>().Ignore(o => o.Editors);
        builder.Entity<AppTemplate>().Ignore(o => o.EditorRoles);
        builder.Entity<AppTemplate>().Ignore(o => o.Editors);
        
        builder.Entity<OwnableResource>().Ignore(m => m.UserRoles);
        builder.Entity<SchemaSeries>().Ignore(m => m.UserRoles);
        builder.Entity<AcquisitionApp>().Ignore(m => m.UserRoles);
        builder.Entity<AppTemplate>().Ignore(m => m.UserRoles);

        builder.Entity<Property>().Ignore(r => r.IsDefaultIdentifier);
        
        // Define navigation dependencies between entities
        builder.Entity<Agent>().HasIndex(a => a.Uri).IsUnique(); // Ensure unique URIs for agents
        builder.Entity<Dataset>().HasOne(d => d.Target).WithMany(d => d.Source);
        builder.Entity<Dataset>().HasMany(d => d.Series).WithMany(d => d.Datasets);
        builder.Entity<Catalogue>().HasOne(c => c.PartOf).WithMany(c => c.Parts);
        builder.Entity<CataloguedResource>().HasOne(c => c.Catalogue)
            .WithMany(c => c.Resources).OnDelete(DeleteBehavior.ClientCascade);
        builder.Entity<Person>().HasOne(p => p.Account)
            .WithOne(u => u.Person).HasForeignKey<Person>();
        builder.Entity<Person>().HasMany(p => p.Organisations).WithMany(o => o.Members)
            .UsingEntity<Membership>(r =>
                    r.HasOne<Organisation>(m => m.Organisation).WithMany(o => o.MemberRoles)
            , l => l.HasOne<Person>(m => m.Person).WithMany(p => p.MemberRoles))
            .ToTable("Members");
        builder.Entity<OwnableResource>().HasOne(mr => mr.ResponsiblePerson)
            .WithMany(p => p.ManagedResources);
        builder.Entity<OwnableResource>().HasOne(mr => mr.ResponsibleOrganisation)
            .WithMany(o => o.ManagedResources);
        builder.Entity<ResourceEditorshipsLink>().HasMany(mr => mr.Editors)
            .WithMany(a => a.ResourceLink)
            .UsingEntity<Editorship>(r => r.HasOne<Agent>(e => e.Agent).
                    WithMany(a => a.EditorRoles)
            , l => l.HasOne<ResourceEditorshipsLink>(e => e.ResourceLink)
                .WithMany(mr => mr.EditorRoles))
            .ToTable("Editors");
        
        builder.Entity<OwnableResource>().HasOne(r => r.EditorshipsLink).WithOne()
            .HasForeignKey<OwnableResource>().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SchemaSeries>().HasOne(r => r.EditorshipsLink).WithOne()
            .HasForeignKey<SchemaSeries>().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AcquisitionApp>().HasOne(r => r.EditorshipsLink).WithOne()
            .HasForeignKey<AcquisitionApp>().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AppTemplate>().HasOne(r => r.EditorshipsLink).WithOne()
            .HasForeignKey<AppTemplate>().OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<AppTemplate>().HasMany(a => a.Versions).WithOne(a => a.VersionOf);
        builder.Entity<AppTemplate>().HasMany(a => a.AuxiliaryConcepts).WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<OwnableResource>().HasIndex(m => m.Uri).IsUnique(); // Ensure unique URIs for resources
        builder.Entity<SchemaSeries>().HasIndex(m => m.Uri).IsUnique(); // Ensure unique URIs for resources
        builder.Entity<AcquisitionApp>().HasIndex(m => m.Uri).IsUnique(); // Ensure unique URIs for resources
        builder.Entity<AppTemplate>().HasIndex(m => m.Uri).IsUnique(); // Ensure unique URIs for resources
        
        builder.Entity<Distribution>().HasOne(d => d.Dataset).WithMany(d => d.Distributions);
        builder.Entity<Distribution>().HasIndex(d => d.Uri).IsUnique(); // Ensure unique URIs for distributions
        builder.Entity<DataService>().HasMany(ds => ds.Distributions).WithOne(d => d.DataService);
        
        builder.Entity<DataSchema>().HasOne(d => d.InSeries).WithMany(s => s.Schemas)
            .OnDelete(DeleteBehavior.ClientCascade);
        builder.Entity<DataSchema>().HasMany(d => d.Concepts).WithOne(c => c.Schema)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<DataSchema>().HasMany(d => d.UsedNamespaces).WithMany();
        builder.Entity<DataSchema>().Ignore(d => d.MainConcept);
        builder.Entity<DataSchema>().Ignore(d => d.DefaultNamespace);
        builder.Entity<Concept>().HasMany(c => c.Properties).WithOne(p => p.Concept)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        // TODO - implement unique constraints for main concepts
        //builder.Entity<Concept>().ToTable(b => b.HasCheckConstraint("CK_Unique_Main_Concept", "???"));
        builder.Entity<AppTemplate>().HasOne(a => a.Schema).WithMany().OnDelete(DeleteBehavior.Restrict);

        //TODO remove unnecessary
        builder.Entity<AcquisitionApp>().HasMany(a => a.SourceResources).WithMany();
        builder.Entity<AcquisitionApp>().HasOne(a => a.ActiveResource).WithMany();
        
        builder.Entity<AcquisitionApp>().HasOne(a => a.Template).WithMany(t => t.Apps)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AppTemplate>().HasMany(t => t.UseCases).WithOne().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AppDataSpecification>().HasOne<AppTemplate>()
            .WithOne(t => t.DataSpecification)
            .HasForeignKey<AppDataSpecification>().OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Standard>().HasMany(s => s.Datasets).WithMany(d => d.ConformsTo);
        builder.Entity<Standard>().HasMany(s => s.DataServices).WithMany(ds => ds.ConformsTo);
        builder.Entity<Standard>().HasIndex(s => s.Uri).IsUnique(); // Ensure unique URIs for standards
        
        builder.Entity<ContactData>().HasIndex(c => c.Uri).IsUnique(); // Ensure unique URIs for contact data
        builder.Entity<Icon>().HasIndex(i => i.Uri).IsUnique(); // Ensure unique URIs for icons
        builder.Entity<SchemaSeries>().HasIndex(s => s.Uri).IsUnique(); // Ensure unique URIs for schema series
        
        // Define properties for handling tables with JSONB columns (PostgreSQL-specific)
        builder.Entity<DataItem>().Property(di => di.Values).HasColumnType("jsonb").IsRequired();
        builder.Entity<DataItem>().HasOne(di => di.Dataset).WithMany(ds => ds.Items);
        builder.Entity<DataItem>().HasOne(di => di.Concept).WithMany();
        builder.Entity<DataItem>().HasOne(di => di.Source).WithMany();
        
        builder.Entity<CodePackage>().HasOne(cp => cp.Template).WithMany(t => t.Packages)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CodePackage>().HasMany(cp => cp.Files).WithOne(cf => cf.Package)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CodePackage>().HasIndex(cp => cp.Uri).IsUnique(); // Ensure unique URIs for code packages
        
        // Custom converters/comparers
        var jsonToTimeTupleCollectionConverter = new ValueConverter<ICollection<(DateTime,DateTime)>, string>(
            c => JsonConvert.SerializeObject(c),
            c => JsonConvert.DeserializeObject<ICollection<(DateTime,DateTime)>>(c) ??
                 new List<(DateTime,DateTime)>());
        var jsonToTimeTupleCollectionComparer = new ValueComparer<ICollection<(DateTime, DateTime)>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());
        
        // Define conversions for some properties
        builder
            .Entity<OwnableResource>()
            .Property(m => m.Description)
            .HasColumnType("jsonb").IsRequired();
        builder
            .Entity<OwnableResource>()
            .Property(m => m.Title)
            .HasColumnType("jsonb").IsRequired();
        builder
            .Entity<Distribution>()
            .Property(d => d.Description)
            .HasColumnType("jsonb").IsRequired();
        builder
            .Entity<Distribution>()
            .Property(d => d.Title)
            .HasColumnType("jsonb").IsRequired();
        builder.Entity<DataResource>().Property(d => d.TemporalCoverage)
            .HasColumnType("jsonb")
            .HasConversion(jsonToTimeTupleCollectionConverter, jsonToTimeTupleCollectionComparer);
    }
}