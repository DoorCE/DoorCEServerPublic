using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEServer.Tests.CataloguingManager.Tests;
using DoorCEServer.Tests.SchemaManager.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

namespace DoorCEServer.Application;

public class DataInitialisation(ApplicationDbContext context, IConfiguration configuration,
    MPopulateDatasetData populateDatasets, MPopulateSchemaData populateSchemas)
{
    public void InitiateDatabase()
    {
        try {
            context.Database.Migrate();
            InitiateAdminAccount();
            InitiateStandardNamespaceList();
            InitiateMainCatalogue();
        }
        catch (Exception ex) {
            Log.Information("<Database> error: {ExMessage}", ex.Message); throw;
        }
    }
    
    public void PopulateDemoDatabase()
    {
        if (1 < context.Catalogues.Count()) {
            Log.Information("<Demo> tried to populate database, but it already contains data - skipping");
            return;
        }
        try {
            populateSchemas.PopulateSchemas();
            populateDatasets.Populate();
            populateSchemas.PopulateApps();
        }
        catch (Exception ex) {
            Log.Information("<Demo> tried to populate database and caused error: {ExMessage}", ex.Message); throw;
        }
    }
    
    public void InitiateMainCatalogue()
    {
        if (context.Catalogues.Any(c => null == c.PartOf)) return;
        populateDatasets.AddMainCatalogue();
        Log.Information("<Database> main catalogue added");
    }

    private void InitiateAdminAccount()
    {
        string userId = configuration["AdminName"] ?? "udas-admin";
        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try {
            if (context.UserAccounts.Any(a => a.UserId == userId)
                || context.UserAccounts.Any(a => a.Roles.Contains(GlobalRole.AgentAdmin))
                && context.UserAccounts.Any(a => a.Roles.Contains(GlobalRole.DataAdmin))) {
                Log.Information("<Database> already contains admin account(s)");
                return;
            }
            // Add admin account
            var adminAccount = new UserAccount()
            {
                UserId = userId,
                Roles = [GlobalRole.AgentAdmin, GlobalRole.DataAdmin ] 
            };
            context.UserAccounts.Add(adminAccount);
            context.SaveChanges();
            transaction.Commit();
            Log.Information("<Database> was populated with admin account: {Id}", userId);
        } catch (Exception ex) {
            context.ChangeTracker.Clear();
            transaction.Rollback();
            Log.Information("<Database> causes admin account error: {ExMessage}", ex.Message);
            throw;
        }
    }

    private void InitiateStandardNamespaceList()
    {
        List<Namespace> standardNamespaces = [
            new Namespace(){
                Iri = "http://www.w3.org/2001/XMLSchema",
                Prefix =  "xsd"
            },
            new Namespace(){
                Iri = "http://www.w3.org/1999/xhtml",
                Prefix = "xhtml"
            },
            new Namespace(){
                Iri = "http://www.w3.org/2000/svg",
                Prefix = "svg"
            },
            new Namespace(){
                Iri = "http://www.w3.org/2002/07/owl#",
                Prefix = "owl"
            },
            new Namespace(){
                Iri = "http://www.w3.org/2004/02/skos/core#",
                Prefix = "skos"
            },
            new Namespace(){
                Iri = "http://www.w3.org/2001/vcard-rdf/3.0#",
                Prefix = "vcard",
            }
        ];
        using IDbContextTransaction transaction = context.Database.BeginTransaction();
        try {
            // Add standard namespaces (if not yet present)
            foreach (Namespace ns in standardNamespaces)
                if (context.Namespaces.All(n => n.Prefix != ns.Prefix))
                    context.Namespaces.Add(ns);
            context.SaveChanges();
            transaction.Commit();
        } catch (Exception) { context.ChangeTracker.Clear(); transaction.Rollback(); throw; }
    }

}