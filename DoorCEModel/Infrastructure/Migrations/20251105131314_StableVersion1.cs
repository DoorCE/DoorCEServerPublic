using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DoorCEServer.Migrations
{
    /// <inheritdoc />
    public partial class StableVersion1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.CreateTable(
                name: "DataFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Icons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Icons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Namespaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Iri = table.Column<string>(type: "text", nullable: false),
                    Prefix = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Namespaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceEditorshipsLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceEditorshipsLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Roles = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    GivenNames = table.Column<string[]>(type: "text[]", nullable: true),
                    FamilyName = table.Column<string>(type: "text", nullable: true),
                    AccountId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agents_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Contents = table.Column<string>(type: "text", nullable: false),
                    AgentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactDatas_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Editors",
                columns: table => new
                {
                    AgentId = table.Column<int>(type: "integer", nullable: false),
                    ResourceLinkId = table.Column<int>(type: "integer", nullable: false),
                    Roles = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Editors", x => new { x.AgentId, x.ResourceLinkId });
                    table.ForeignKey(
                        name: "FK_Editors_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Editors_ResourceEditorshipsLinks_ResourceLinkId",
                        column: x => x.ResourceLinkId,
                        principalTable: "ResourceEditorshipsLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    OrganisationId = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Roles = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => new { x.OrganisationId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_Members_Agents_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Members_Agents_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcquisitionAppDataResource",
                columns: table => new
                {
                    AcquisitionAppId = table.Column<int>(type: "integer", nullable: false),
                    SourceResourcesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionAppDataResource", x => new { x.AcquisitionAppId, x.SourceResourcesId });
                });

            migrationBuilder.CreateTable(
                name: "AcquisitionApps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    ActiveResourceId = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionApps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcquisitionApps_ResourceEditorshipsLinks_Id",
                        column: x => x.Id,
                        principalTable: "ResourceEditorshipsLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppDataSpecification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Contents = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDataSpecification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    VersionOfId = table.Column<int>(type: "integer", nullable: true),
                    SchemaId = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    IsReady = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTemplates_AppTemplates_VersionOfId",
                        column: x => x.VersionOfId,
                        principalTable: "AppTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppTemplates_ResourceEditorshipsLinks_Id",
                        column: x => x.Id,
                        principalTable: "ResourceEditorshipsLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UseCaseName = table.Column<string>(type: "text", nullable: false),
                    ScenariosContents = table.Column<string>(type: "text", nullable: false),
                    AppTemplateId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UseCaseScenarios_AppTemplates_AppTemplateId",
                        column: x => x.AppTemplateId,
                        principalTable: "AppTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactDataOwnableResource",
                columns: table => new
                {
                    ContactsId = table.Column<int>(type: "integer", nullable: false),
                    ResourcesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactDataOwnableResource", x => new { x.ContactsId, x.ResourcesId });
                    table.ForeignKey(
                        name: "FK_ContactDataOwnableResource_ContactDatas_ContactsId",
                        column: x => x.ContactsId,
                        principalTable: "ContactDatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConceptUri = table.Column<string>(type: "text", nullable: true),
                    Values = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    DatasetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataSchemaNamespace",
                columns: table => new
                {
                    DataSchemaId = table.Column<int>(type: "integer", nullable: false),
                    UsedNamespacesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSchemaNamespace", x => new { x.DataSchemaId, x.UsedNamespacesId });
                    table.ForeignKey(
                        name: "FK_DataSchemaNamespace_Namespaces_UsedNamespacesId",
                        column: x => x.UsedNamespacesId,
                        principalTable: "Namespaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataServiceStandard",
                columns: table => new
                {
                    ConformsToId = table.Column<int>(type: "integer", nullable: false),
                    DataServicesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataServiceStandard", x => new { x.ConformsToId, x.DataServicesId });
                });

            migrationBuilder.CreateTable(
                name: "DatasetDatasetSeries",
                columns: table => new
                {
                    DatasetsId = table.Column<int>(type: "integer", nullable: false),
                    SeriesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetDatasetSeries", x => new { x.DatasetsId, x.SeriesId });
                });

            migrationBuilder.CreateTable(
                name: "DatasetStandard",
                columns: table => new
                {
                    ConformsToId = table.Column<int>(type: "integer", nullable: false),
                    DatasetsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetStandard", x => new { x.ConformsToId, x.DatasetsId });
                });

            migrationBuilder.CreateTable(
                name: "Distributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    Description = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AccessUrl = table.Column<string[]>(type: "text[]", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: true),
                    AccessStatus = table.Column<short>(type: "smallint", nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: true),
                    Languages = table.Column<string[]>(type: "text[]", nullable: false),
                    MediaType = table.Column<string>(type: "text", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DatasetId = table.Column<int>(type: "integer", nullable: false),
                    DataServiceId = table.Column<int>(type: "integer", nullable: true),
                    SchemaId = table.Column<int>(type: "integer", nullable: true),
                    Checksum = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    CompressionFormat = table.Column<string>(type: "text", nullable: true),
                    DownloadUrl = table.Column<string>(type: "text", nullable: true),
                    FileId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    NamespaceId = table.Column<int>(type: "integer", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: true),
                    SchemaId = table.Column<int>(type: "integer", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: true),
                    Multiple = table.Column<bool>(type: "boolean", nullable: true),
                    Unique = table.Column<bool>(type: "boolean", nullable: true),
                    ConceptId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: true),
                    TypeId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamespaceElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamespaceElements_NamespaceElements_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "NamespaceElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceElements_NamespaceElements_TypeId",
                        column: x => x.TypeId,
                        principalTable: "NamespaceElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceElements_Namespaces_NamespaceId",
                        column: x => x.NamespaceId,
                        principalTable: "Namespaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OwnableResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    Description = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    IconUri = table.Column<string>(type: "text", nullable: true),
                    ResponsiblePersonId = table.Column<int>(type: "integer", nullable: true),
                    ResponsibleOrganisationId = table.Column<int>(type: "integer", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    PartOfId = table.Column<int>(type: "integer", nullable: true),
                    AccessRights = table.Column<short>(type: "smallint", nullable: true),
                    Languages = table.Column<string[]>(type: "text[]", nullable: true),
                    Keywords = table.Column<string[]>(type: "text[]", nullable: true),
                    Themes = table.Column<string[]>(type: "text[]", nullable: true),
                    ApplicableLegislations = table.Column<string[]>(type: "text[]", nullable: true),
                    Licences = table.Column<string[]>(type: "text[]", nullable: true),
                    CatalogueId = table.Column<int>(type: "integer", nullable: true),
                    GeographicalCoverage = table.Column<string[]>(type: "text[]", nullable: true),
                    Frequency = table.Column<string>(type: "text", nullable: true),
                    TemporalCoverage = table.Column<string>(type: "jsonb", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Type = table.Column<short[]>(type: "smallint[]", nullable: true),
                    Dataset_Status = table.Column<short>(type: "smallint", nullable: true),
                    Dataset_Documentation = table.Column<string[]>(type: "text[]", nullable: true),
                    Provenance = table.Column<string[]>(type: "text[]", nullable: true),
                    Version = table.Column<string>(type: "text", nullable: true),
                    VersionNotes = table.Column<string>(type: "text", nullable: true),
                    VersionOfId = table.Column<int>(type: "integer", nullable: true),
                    SchemaId = table.Column<int>(type: "integer", nullable: true),
                    TargetId = table.Column<int>(type: "integer", nullable: true),
                    EndpointUrl = table.Column<string[]>(type: "text[]", nullable: true),
                    EndpointDescription = table.Column<string[]>(type: "text[]", nullable: true),
                    Documentation = table.Column<string[]>(type: "text[]", nullable: true),
                    Format = table.Column<string[]>(type: "text[]", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnableResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnableResources_Agents_ResponsibleOrganisationId",
                        column: x => x.ResponsibleOrganisationId,
                        principalTable: "Agents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OwnableResources_Agents_ResponsiblePersonId",
                        column: x => x.ResponsiblePersonId,
                        principalTable: "Agents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OwnableResources_OwnableResources_CatalogueId",
                        column: x => x.CatalogueId,
                        principalTable: "OwnableResources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OwnableResources_OwnableResources_PartOfId",
                        column: x => x.PartOfId,
                        principalTable: "OwnableResources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OwnableResources_OwnableResources_TargetId",
                        column: x => x.TargetId,
                        principalTable: "OwnableResources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OwnableResources_OwnableResources_VersionOfId",
                        column: x => x.VersionOfId,
                        principalTable: "OwnableResources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OwnableResources_ResourceEditorshipsLinks_Id",
                        column: x => x.Id,
                        principalTable: "ResourceEditorshipsLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchemaSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<short[]>(type: "smallint[]", nullable: false),
                    CurrentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemaSeries_ResourceEditorshipsLinks_Id",
                        column: x => x.Id,
                        principalTable: "ResourceEditorshipsLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Standards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    InSeriesId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Standards_SchemaSeries_InSeriesId",
                        column: x => x.InSeriesId,
                        principalTable: "SchemaSeries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionAppDataResource_SourceResourcesId",
                table: "AcquisitionAppDataResource",
                column: "SourceResourcesId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionApps_ActiveResourceId",
                table: "AcquisitionApps",
                column: "ActiveResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionApps_TemplateId",
                table: "AcquisitionApps",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionApps_Uri",
                table: "AcquisitionApps",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_AccountId",
                table: "Agents",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Uri",
                table: "Agents",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTemplates_SchemaId",
                table: "AppTemplates",
                column: "SchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTemplates_Uri",
                table: "AppTemplates",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTemplates_VersionOfId",
                table: "AppTemplates",
                column: "VersionOfId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactDataOwnableResource_ResourcesId",
                table: "ContactDataOwnableResource",
                column: "ResourcesId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactDatas_AgentId",
                table: "ContactDatas",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactDatas_Uri",
                table: "ContactDatas",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataItems_DatasetId",
                table: "DataItems",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSchemaNamespace_UsedNamespacesId",
                table: "DataSchemaNamespace",
                column: "UsedNamespacesId");

            migrationBuilder.CreateIndex(
                name: "IX_DataServiceStandard_DataServicesId",
                table: "DataServiceStandard",
                column: "DataServicesId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetDatasetSeries_SeriesId",
                table: "DatasetDatasetSeries",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetStandard_DatasetsId",
                table: "DatasetStandard",
                column: "DatasetsId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_DataServiceId",
                table: "Distributions",
                column: "DataServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_DatasetId",
                table: "Distributions",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_SchemaId",
                table: "Distributions",
                column: "SchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_Uri",
                table: "Distributions",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Editors_ResourceLinkId",
                table: "Editors",
                column: "ResourceLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Icons_Uri",
                table: "Icons",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_PersonId",
                table: "Members",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceElements_ConceptId",
                table: "NamespaceElements",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceElements_NamespaceId",
                table: "NamespaceElements",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceElements_SchemaId",
                table: "NamespaceElements",
                column: "SchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceElements_TypeId",
                table: "NamespaceElements",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnableResources_CatalogueId",
                table: "OwnableResources",
                column: "CatalogueId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnableResources_PartOfId",
                table: "OwnableResources",
                column: "PartOfId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnableResources_ResponsibleOrganisationId",
                table: "OwnableResources",
                column: "ResponsibleOrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnableResources_ResponsiblePersonId",
                table: "OwnableResources",
                column: "ResponsiblePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnableResources_SchemaId",
                table: "OwnableResources",
                column: "SchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnableResources_TargetId",
                table: "OwnableResources",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnableResources_Uri",
                table: "OwnableResources",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnableResources_VersionOfId",
                table: "OwnableResources",
                column: "VersionOfId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaSeries_CurrentId",
                table: "SchemaSeries",
                column: "CurrentId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaSeries_Uri",
                table: "SchemaSeries",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Standards_InSeriesId",
                table: "Standards",
                column: "InSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Standards_Uri",
                table: "Standards",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseScenarios_AppTemplateId",
                table: "UseCaseScenarios",
                column: "AppTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcquisitionAppDataResource_AcquisitionApps_AcquisitionAppId",
                table: "AcquisitionAppDataResource",
                column: "AcquisitionAppId",
                principalTable: "AcquisitionApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AcquisitionAppDataResource_OwnableResources_SourceResources~",
                table: "AcquisitionAppDataResource",
                column: "SourceResourcesId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AcquisitionApps_AppTemplates_TemplateId",
                table: "AcquisitionApps",
                column: "TemplateId",
                principalTable: "AppTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AcquisitionApps_OwnableResources_ActiveResourceId",
                table: "AcquisitionApps",
                column: "ActiveResourceId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppDataSpecification_AppTemplates_Id",
                table: "AppDataSpecification",
                column: "Id",
                principalTable: "AppTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppTemplates_Standards_SchemaId",
                table: "AppTemplates",
                column: "SchemaId",
                principalTable: "Standards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactDataOwnableResource_OwnableResources_ResourcesId",
                table: "ContactDataOwnableResource",
                column: "ResourcesId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataItems_OwnableResources_DatasetId",
                table: "DataItems",
                column: "DatasetId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataSchemaNamespace_Standards_DataSchemaId",
                table: "DataSchemaNamespace",
                column: "DataSchemaId",
                principalTable: "Standards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataServiceStandard_OwnableResources_DataServicesId",
                table: "DataServiceStandard",
                column: "DataServicesId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataServiceStandard_Standards_ConformsToId",
                table: "DataServiceStandard",
                column: "ConformsToId",
                principalTable: "Standards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetDatasetSeries_OwnableResources_DatasetsId",
                table: "DatasetDatasetSeries",
                column: "DatasetsId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetDatasetSeries_OwnableResources_SeriesId",
                table: "DatasetDatasetSeries",
                column: "SeriesId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetStandard_OwnableResources_DatasetsId",
                table: "DatasetStandard",
                column: "DatasetsId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetStandard_Standards_ConformsToId",
                table: "DatasetStandard",
                column: "ConformsToId",
                principalTable: "Standards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Distributions_OwnableResources_DataServiceId",
                table: "Distributions",
                column: "DataServiceId",
                principalTable: "OwnableResources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributions_OwnableResources_DatasetId",
                table: "Distributions",
                column: "DatasetId",
                principalTable: "OwnableResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Distributions_Standards_SchemaId",
                table: "Distributions",
                column: "SchemaId",
                principalTable: "Standards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NamespaceElements_Standards_SchemaId",
                table: "NamespaceElements",
                column: "SchemaId",
                principalTable: "Standards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnableResources_Standards_SchemaId",
                table: "OwnableResources",
                column: "SchemaId",
                principalTable: "Standards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SchemaSeries_Standards_CurrentId",
                table: "SchemaSeries",
                column: "CurrentId",
                principalTable: "Standards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SchemaSeries_ResourceEditorshipsLinks_Id",
                table: "SchemaSeries");

            migrationBuilder.DropForeignKey(
                name: "FK_SchemaSeries_Standards_CurrentId",
                table: "SchemaSeries");

            migrationBuilder.DropTable(
                name: "AcquisitionAppDataResource");

            migrationBuilder.DropTable(
                name: "AppDataSpecification");

            migrationBuilder.DropTable(
                name: "ContactDataOwnableResource");

            migrationBuilder.DropTable(
                name: "DataFiles");

            migrationBuilder.DropTable(
                name: "DataItems");

            migrationBuilder.DropTable(
                name: "DataSchemaNamespace");

            migrationBuilder.DropTable(
                name: "DataServiceStandard");

            migrationBuilder.DropTable(
                name: "DatasetDatasetSeries");

            migrationBuilder.DropTable(
                name: "DatasetStandard");

            migrationBuilder.DropTable(
                name: "Distributions");

            migrationBuilder.DropTable(
                name: "Editors");

            migrationBuilder.DropTable(
                name: "Icons");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "NamespaceElements");

            migrationBuilder.DropTable(
                name: "UseCaseScenarios");

            migrationBuilder.DropTable(
                name: "AcquisitionApps");

            migrationBuilder.DropTable(
                name: "ContactDatas");

            migrationBuilder.DropTable(
                name: "Namespaces");

            migrationBuilder.DropTable(
                name: "AppTemplates");

            migrationBuilder.DropTable(
                name: "OwnableResources");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropTable(
                name: "ResourceEditorshipsLinks");

            migrationBuilder.DropTable(
                name: "Standards");

            migrationBuilder.DropTable(
                name: "SchemaSeries");
        }
    }
}
