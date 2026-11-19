# Changelog

All notable changes to KemiKoll are listed here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- README, contributing guide, roadmap, changelog and architecture decision records.
- GNU AGPL v3 license.

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

[Unreleased]: https://github.com/aobeid1979/KemiKoll/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/aobeid1979/KemiKoll/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/aobeid1979/KemiKoll/releases/tag/v0.2.0
