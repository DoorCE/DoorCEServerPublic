namespace DoorCEServer.Application.CkanProxy.Common;

public static class CkanActionNames {
    public const string PackageCreate = "package_create";
    public const string PackageUpdate = "package_update";
    public const string PackageShow = "package_show";
    public const string DatasetPurge =  "dataset_purge";

    public const string OrganizationCreate = "organization_create";
    public const string OrganizationUpdate = "organization_update";
    public const string OrganizationShow = "organization_show";
    public const string OrganizationDelete = "organization_delete";

    public const string ResourceCreate = "resource_create";
    public const string ResourceDelete = "resource_delete";

    public const string DatastoreSearch = "datastore_search";
    public const string DatastoreCreate = "datastore_create";
    public const string DatastoreUpsert = "datastore_upsert";
    public const string DatastoreDelete = "datastore_delete";
}