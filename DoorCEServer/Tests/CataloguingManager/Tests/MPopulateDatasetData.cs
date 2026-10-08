using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using Serilog;

namespace DoorCEServer.Tests.CataloguingManager.Tests;

public class MPopulateDatasetData(TestCommon testCommon, AgentAPI agentApi, DatasetManagementAPI datasetApi,
    DistributionManagementAPI distributionApi)
{
    private static readonly Dictionary<string, string> MainCatalogue = new() {
        { "en", "Main" },
        { "pl", "Główny" },
        { "it", "Principale" },
        { "sk", "Hlavný" },
        { "sl", "Glavni" },
        { "hr", "Glavni" },
        { "de", "Haupt" }
    };

    private static readonly Dictionary<string, string> EnvironmentCatalogue = new() {
        { "en", "Environment" },
        { "pl", "Środowisko" },
        { "it", "Ambiente" },
        { "sk", "Životné prostredie" },
        { "sl", "Okolje" },
        { "hr", "Okoliš" },
        { "de", "Umwelt" }
    };
    
    private static readonly Dictionary<string, string> TransportCatalogue = new() {
        { "en", "Transport" },
        { "pl", "Transport" },
        { "it", "Trasporti" },
        { "sk", "Doprava" },
        { "sl", "Promet" },
        { "hr", "Promet" },
        { "de", "Verkehr" }
    };
    
    private static readonly Dictionary<string, string> TreesInParksDataset = new() {
        { "en", "Trees in parks" },
        { "pl", "Drzewa w parkach" },
        { "it", "Alberi nei parchi" },
        { "sk", "Stromy v parkoch" },
        { "sl", "Drevesa v parkih" },
        { "hr", "Drveće u parkovima" },
        { "de", "Bäume in Parks" }
    };

    private static readonly Dictionary<string, string> TreesInForestsDataset = new() {
        { "en", "Trees in forests" },
        { "pl", "Drzewa w lasach" },
        { "it", "Alberi nelle foreste" },
        { "sk", "Stromy v lesoch" },
        { "sl", "Drevesa v gozdovih" },
        { "hr", "Drveće u šumama" },
        { "de", "Bäume in Wäldern" }
    };

    private static readonly Dictionary<string, string> TreesInCityDataset = new() {
        { "en", "Trees in city" },
        { "pl", "Drzewa w mieście" },
        { "it", "Alberi in città" },
        { "sk", "Stromy v meste" },
        { "sl", "Drevesa v mestu" },
        { "hr", "Drveće u gradu" },
        { "de", "Bäume in der Stadt" }
    };
    
    private static readonly Dictionary<string, string> TramLinesDataset = new() {
        { "en", "Tram lines" },
        { "pl", "Linie tramwajowe" },
        { "it", "Linee tramviarie" },
        { "sk", "Električkové linky" },
        { "sl", "Tramvajske linije" },
        { "hr", "Tramvajske linije" },
        { "de", "Straßenbahnlinien" }
    };

    private static readonly Dictionary<string, string> BusLinesDataset = new() {
        { "en", "Bus lines" },
        { "pl", "Linie autobusowe" },
        { "it", "Linee di autobus" },
        { "sk", "Autobusové linky" },
        { "sl", "Avtobusne linije" },
        { "hr", "Autobusne linije" },
        { "de", "Buslinien" }
    };

    private static readonly Dictionary<string, string> PublicTransportLinesDataset = new() {
        { "en", "Public transport lines" },
        { "pl", "Linie komunikacji publicznej" },
        { "it", "Linee di trasporto pubblico" },
        { "sk", "Linky verejnej dopravy" },
        { "sl", "Linije javnega prevoza" },
        { "hr", "Linije javnog prijevoza" },
        { "de", "Linien des öffentlichen Nahverkehrs" }
    };
    
    private static readonly Dictionary<string, string> TransportService = new() {
        { "en", "Transport service" },
        { "pl", "Usługa transportowa" },
        { "it", "Servizio di trasporto" },
        { "sk", "Dopravná služba" },
        { "sl", "Prometna storitev" },
        { "hr", "Prometna usluga" },
        { "de", "Verkehrsdienst" }
    };

    private static readonly Dictionary<string, string> EnvironmentService = new() {
        { "en", "Environment service" },
        { "pl", "Usługa środowiskowa" },
        { "it", "Servizio ambientale" },
        { "sk", "Environmentálna služba" },
        { "sl", "Okoljska storitev" },
        { "hr", "Usluga zaštite okoliša" },
        { "de", "Umweltdienst" }
    };

    private static readonly Dictionary<string, string> CityFacilitiesService = new() {
        { "en", "City facilities service" },
        { "pl", "Usługa infrastruktury miejskiej" },
        { "it", "Servizio infrastrutture urbane" },
        { "sk", "Služba mestskej infraštruktúry" },
        { "sl", "Storitev mestne infrastrukture" },
        { "hr", "Usluga gradske infrastrukture" },
        { "de", "Stadtinfrastrukturdienst" }
    };
    
    private static readonly Dictionary<string, string> FullTreeData = new() {
        { "en", "Full tree data" },
        { "pl", "Pełne dane drzew" },
        { "it", "Dati completi degli alberi" },
        { "sk", "Úplné údaje o stromoch" },
        { "sl", "Popolni podatki o drevesih" },
        { "hr", "Potpuni podaci o drveću" },
        { "de", "Vollständige Baumdaten" }
    };

    private static readonly Dictionary<string, string> SelectedTreeData = new() {
        { "en", "Selected tree data" },
        { "pl", "Wybrane dane drzew" },
        { "it", "Dati selezionati degli alberi" },
        { "sk", "Vybrané údaje o stromoch" },
        { "sl", "Izbrani podatki o drevesih" },
        { "hr", "Odabrani podaci o drveću" },
        { "de", "Ausgewählte Baumdaten" }
    };
    
    public void AddMainCatalogue()
    {
        var xCatalogue = testCommon.GetMockXCatalogue("cat/main", MainCatalogue,
            null, null, null,[]);
        datasetApi.UpsertCatalogue(xCatalogue, [], "udas-admin");
    }

    public void Populate()
    {
        // ****** Add agents ******
        List<XContactData> xContacts = testCommon.GetMockContacts("cnt/wut", ["biuro@wut.org", "+48 123 456 789"]);
        XOrganisation xOrganisation = testCommon.GetMockXOrganisation("org/WUT", "WUT", new List<string>());
        agentApi.UpsertOrganisation(xOrganisation, xContacts, "udas-admin");
        
        xContacts = testCommon.GetMockContacts("cnt/kowalski", ["kowalski@wut.org", "+48 987 654 321"]);
        var xPerson = testCommon.GetMockXPerson("prs/kowalski", "Kowalski", new List<string>(),
            ["org/WUT"]);
        agentApi.UpsertPerson(xPerson, xContacts, "udas-admin");
        
        xContacts = testCommon.GetMockContacts("cnt/nowak", ["nowak@wut.org", "+48 111 222 333"]);
        xPerson = testCommon.GetMockXPerson("prs/nowak", "Nowak", new List<string>(),
            ["org/WUT"]);
        agentApi.UpsertPerson(xPerson, xContacts, "udas-admin");
        
        xContacts = testCommon.GetMockContacts("cnt/simon", ["simon@wut.org", "+48 444 555 666"]);
        xPerson = testCommon.GetMockXPerson("prs/simon", "Simon", new List<string>(),
            new List<string>());
        agentApi.UpsertPerson(xPerson, xContacts, "udas-admin");
        
        // ****** Add users ******
        try {
            agentApi.UpsertUserAccount(new XUserAccount
            {
                UserId = "asimon", PersonUri = "prs/simon", Roles = [GlobalRole.DataAdmin]
            }, "udas-admin");
        } catch (ArgumentException){
            Log.Warning("User ID [asimon] does not exist in Keycloak - account not created (can be created by editing Person with ID [prs/simon])");
        }

        try {
            agentApi.UpsertUserAccount(new XUserAccount
            {
                UserId = "anowak", PersonUri = "prs/nowak"
            }, "udas-admin");
        } catch (ArgumentException){
            Log.Warning("User ID [anowak] does not exist in Keycloak - account not created (can be created by editing Person with ID [prs/nowak])");
        }

        // ****** Add catalogues ******
        var xCatalogue = testCommon.GetMockXCatalogue("cat/environment", EnvironmentCatalogue,
            "org/WUT", "prs/kowalski", "cat/main", ["cnt/kowalski1"]);
        xContacts = testCommon.GetMockContacts("cnt/contact_b", ["jan@wut.org", "+48 777 888 999"]);
        datasetApi.UpsertCatalogue(xCatalogue, xContacts, "udas-admin");
        
        xCatalogue = testCommon.GetMockXCatalogue("cat/transport", TransportCatalogue,
            "org/WUT", "prs/kowalski", "cat/main", ["cnt/kowalski1"]);
        xContacts = testCommon.GetMockContacts("cnt/contact_j", ["jan@wut.org", "+48 777 888 999"]);
        datasetApi.UpsertCatalogue(xCatalogue, xContacts,"udas-admin");
        
        // ****** Add dataset series ******
        XDatasetSeries xSeries = testCommon.GetMockXSeries("srs/trees_in_city", 
            TreesInCityDataset,
            "org/WUT", "prs/kowalski", "cat/main");
        xContacts = testCommon.GetMockContacts("cnt/contact_d", ["anna@wut.org", "+48 333 444 555"]);
        datasetApi.UpsertSeries(xSeries, xContacts,"udas-admin");

        // ****** Add datasets ******
        
        // *** Environment datasets
        XDataset xDataset = testCommon.GetMockXDataset("dat/trees_in_parks/1.0", 
            TreesInParksDataset,
            "cat/environment", "org/WUT", "prs/kowalski",
            new List<string>() { "srs/trees_in_city" }, "sch/tree_data_schema_v2_", true);
        xContacts = testCommon.GetMockContacts("cnt/contact_e", ["stan@wut.org", "+48 678 888 999"]);
        datasetApi.UpsertDataset(xDataset, xContacts,"udas-admin");
        
        xDataset = testCommon.GetMockXDataset("dat/trees_in_forests/1.0", 
        TreesInForestsDataset,
            "cat/environment", "org/WUT", "prs/kowalski",
            new List<string>() { "srs/trees_in_city" }, "sch/tree_data_schema_v2_", true);
        xDataset.EditorsUris = new List<string>() { "prs/nowak" };
        xDataset.EditorsRoles = new Dictionary<string, ICollection<EditorRole>> {
            { "prs/nowak", new List<EditorRole>() { EditorRole.DistributionEditor, EditorRole.MetadataEditor } }
        };
        xContacts = testCommon.GetMockContacts("cnt/contact_i", ["stan@wut.org", "+48 678 888 999"]);
        datasetApi.UpsertDataset(xDataset, xContacts,"udas-admin");
        
        xDataset = testCommon.GetMockXDataset("dat/trees_in_city/1.0", 
            TreesInCityDataset,
            "cat/main", "org/WUT", "prs/kowalski",
            new List<string>(), "sch/tree_data_schema_v2_");
        xDataset.EditorsUris = new List<string>() { "prs/nowak" };
        xDataset.EditorsRoles = new Dictionary<string, ICollection<EditorRole>> {
            { "prs/nowak", new List<EditorRole>() { EditorRole.DistributionEditor, EditorRole.MetadataEditor } }
        };
        datasetApi.UpsertDataset(xDataset, xContacts,"udas-admin");
        
        // *** Transport datasets
        xDataset = testCommon.GetMockXDataset("dat/bus_lines/1.0", 
            BusLinesDataset,
            "cat/transport", "org/WUT", "prs/kowalski",
            null, null, true);
        xContacts = testCommon.GetMockContacts("cnt/contact_e", ["stan@wut.org", "+48 678 888 999"]);
        datasetApi.UpsertDataset(xDataset, xContacts,"udas-admin");
        
        xDataset = testCommon.GetMockXDataset("dat/tram_lines/1.0", 
            TramLinesDataset,
            "cat/transport", "org/WUT", "prs/kowalski",
            null, null, true);
        xDataset.EditorsUris = new List<string>() { "prs/nowak" };
        xDataset.EditorsRoles = new Dictionary<string, ICollection<EditorRole>> {
            { "prs/nowak", new List<EditorRole>() { EditorRole.DistributionEditor, EditorRole.MetadataEditor } }
        };
        xContacts = testCommon.GetMockContacts("cnt/contact_i", ["stan@wut.org", "+48 678 888 999"]);
        datasetApi.UpsertDataset(xDataset, xContacts,"udas-admin");
        
        xDataset = testCommon.GetMockXDataset("dat/public_transport_lines/1.0", 
            PublicTransportLinesDataset,
            "cat/main", "org/WUT", "prs/kowalski");
        xDataset.EditorsUris = new List<string>() { "prs/nowak" };
        xDataset.EditorsRoles = new Dictionary<string, ICollection<EditorRole>> {
            { "prs/nowak", new List<EditorRole>() { EditorRole.DistributionEditor, EditorRole.MetadataEditor } }
        };
        datasetApi.UpsertDataset(xDataset, xContacts,"udas-admin");
        
        // ****** Add data services ******
        xContacts = testCommon.GetMockContacts("cnt/contact_f", ["jan@wut.org", "+48 777 888 999"]);
        distributionApi.UpsertDataService(testCommon.GetMockXDataService("srv/data_service_1",
            EnvironmentService,
                "cat/environment", "org/WUT", "prs/kowalski"), xContacts,
            "udas-admin");
        xContacts = testCommon.GetMockContacts("cnt/contact_g", ["jan@wut.org", "+48 777 888 999"]);
        distributionApi.UpsertDataService(testCommon.GetMockXDataService("srv/data_service_2",
            TransportService,
                "cat/transport", "org/WUT", "prs/kowalski"), xContacts,
            "udas-admin");
        distributionApi.UpsertDataService(testCommon.GetMockXDataService("srv/data_service_2",
                CityFacilitiesService,
                "cat/main", "org/WUT", "prs/kowalski"), xContacts,
            "udas-admin");
        
        // ****** Add distributions to datasets ******
        distributionApi.UpsertDistribution(testCommon.GetMockXDistribution("dis/full_tree_data",
            FullTreeData,
                "dat/trees_in_parks/1.0", "srv/data_service_1"),
            "udas-admin");
        distributionApi.UpsertDistribution(testCommon.GetMockXDistribution("dis/selected_tree_data",
            SelectedTreeData,
                "dat/trees_in_parks/1.0", "srv/data_service_2"),
            "udas-admin");
    }
}