# Changelog

All notable changes to KemiKoll are listed here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Read the text of PDF files (`IPdfTextReader`, PdfPig 0.1.16) with limits for untrusted uploads: 20 MB, 100 pages. (#22)
- Find hazard codes, pictogram codes, revision date, language, product name and supplier in a safety data sheet with fixed rules (`SafetyDataSheetTextParser`, `ReadSafetyDataSheet`). (#23)
- Upload a safety data sheet PDF on the "Add product" page: the form is filled in with the suggestions; the data sheet (revision date, language, file name) is saved together with the product.

- CLP pictogram rules (`PictogramRules`): required pictograms from the H-codes with the Article 26 precedence rules. Used to suggest pictograms when a data sheet shows them only as images, to check the form live (with "Use the pictograms that match the H-codes"), and to show "Missing / Not expected" in the product list. ([decision 0007](docs/decisions/0007-pictograms-from-hazard-statements.md))

- **Edit and delete products**: "Edit" link in the product list, edit page with the same form and CLP check, delete with confirmation (data sheets are deleted too).
- **Data sheets on the edit page**: list (newest first, older kept as history), correct revision date and language, remove with confirmation, add a newer version by uploading its PDF — with an offer to take over its H-codes and pictograms when they differ.

- Products remember when they were added and last edited (`AddedAt`, `LastChangedAt`; migration `AddProductDates`, existing products get the migration time). Shown on the edit page. Saving without changes keeps the old date.
- "Export to Excel" exports what the list shows: the current search and quick filter are used, and written under the title in the file.
- "Export to Excel" on the product list: downloads the whole inventory (kemikalieförteckning) as an .xlsx file with title, export date, filter buttons and a frozen header row (`ExportInventory`, `OpenXmlInventorySpreadsheetWriter`, DocumentFormat.OpenXml 3.5.1, MIT).
- Quick filters above the product list (All, Needs attention, Hazardous) with counts; they replace the summary line and work together with the search box.
- Search box above the product list: filters by name, supplier, hazard code or pictogram code while typing; several words must all match.
- Duplicate warning when adding a product with the same name and supplier as an existing one, with a link to it and a "Save anyway" button.
- Startup check of the database: if it cannot be reached or is missing migrations, the app stops with one clear message and the fix (instead of a long error on the first page).

### Changed
- `AddProduct` can save a data sheet together with the product; the data sheet is validated before anything is saved.
- The product fields and the pictogram check are a shared component (`ProductFormFields`), used by the Add and Edit pages.
- Data sheet extraction tested with real files and improved: dates with month names ("25 January 2021", "25 januari 2021"), more revision-date labels ("Effective date", "Date of latest revision", "Senast reviderad" …), "Product identifier" as fallback for the name, supplier chosen by company-name ending (AB, Ltd., GmbH …), table layouts with label and value on one line.
- New warnings: document is not a safety data sheet (e.g. a technical data sheet); no H-codes in section 2.

### Fixed
- Words like "supplier's" in running text were taken as the supplier label.
- Supplier is now found under the section 1.3 heading when there is no "Supplier:" label (common in Swedish data sheets).
- A second field on the same line ("Solvent Version: 8") is no longer added to the product name.

## [0.4.0] - 2026-11-21

Data is stored in PostgreSQL and survives a restart.

### Added
- README, contributing guide, roadmap, changelog and architecture decision records.
- GNU AGPL v3 license.
- Local PostgreSQL 17 database in Docker (`docker-compose.yml`, `.env.example`).
- EF Core with PostgreSQL: `KemiKollDbContext` and table mappings for products and safety data sheets.
- `KemiKoll.Infrastructure.Tests` with tests for the database mapping.
- EF Core migrations: `InitialCreate`, `dotnet ef` tool pinned in `.config/dotnet-tools.json`, CI check for missing migrations. (#20)
- **The app stores products and data sheets in PostgreSQL** when the connection string `KemiKoll` is set (User Secrets in Development); otherwise in memory.
- Integration tests against a real PostgreSQL container (Testcontainers).

### Changed
- Example data is only added when the inventory is empty.

## [0.3.0] - 2026-11-19

First version with a user interface.

### Added
- Product list page (`/products`) with name, supplier, hazard statements and hazardous yes/no. (#13)
- Navigation bar and base styles. (#13)
- "Add product" form (`/products/new`) with validation messages. (#14)
- Safety data sheet status per product: up to date, due for review, missing, not required. (#15)
- `AddSafetyDataSheet` use case and in-memory data sheet storage. (#15)
- Official GHS pictogram images in the product list and the form. (#16)
- `THIRD-PARTY-NOTICES.md` for the pictogram files. (#16)
- `AddProduct` and `ListProducts` use cases with in-memory product storage. (#12)
- Example products in Development mode, covering every data sheet status. (#13, #15)

## [0.2.0] - 2026-11-16

The domain model: all business rules, no database or UI.

### Added
- `HazardStatement`: H-phrase codes with CLP category. (#7)
- `ChemicalProduct`: name, supplier, hazard statements, pictograms. (#8)
- `SafetyDataSheet` and `ReviewPolicy` (company-defined review age, default 3 years). (#9)
- `Pictogram` with official CLP Annex V names, and `PictogramCodes` for GHS01–GHS09. (#10)
- `OrganizationNumber` (Swedish, Luhn check), `Company` and `Workplace`. (#11)

### Project setup
- Clean Architecture solution on .NET 10, xUnit tests with architecture rules,
  shared build rules, GitHub Actions CI and branch protection. (#1–#6)

[Unreleased]: https://github.com/aobeid1979/KemiKoll/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/aobeid1979/KemiKoll/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/aobeid1979/KemiKoll/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/aobeid1979/KemiKoll/releases/tag/v0.2.0
