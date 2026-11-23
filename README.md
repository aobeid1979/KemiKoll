# KemiKoll

[![CI](https://github.com/aobeid1979/KemiKoll/actions/workflows/ci.yml/badge.svg)](https://github.com/aobeid1979/KemiKoll/actions/workflows/ci.yml)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE)

**Chemical inventory and safety data sheet management for small workshops in Sweden.**

Swedish workplaces that use chemical products must keep a chemical inventory
(*kemikalieförteckning*) and a safety data sheet (*säkerhetsdatablad*) for every
hazardous product. Car workshops, CNC shops, hair salons and cleaning companies
usually manage this with scattered PDF files and spreadsheets.

KemiKoll keeps it in one place: which products you have, how hazardous they are,
and whether each data sheet is present and up to date. The goal is that a workshop
uploads its data sheets as PDF files and an AI agent fills in the inventory.

> **Status:** early development — version **0.4.0**. See the [roadmap](docs/roadmap.md).
>
> KemiKoll is a support tool. It does not replace the employer's legal responsibility
> or professional advice.

## What works today

- **Product list** with name, supplier, hazard statements (H-phrases) and the official
  GHS pictograms.
- **Data sheet status** for every product:
  🟢 up to date · 🟠 due for review (older than 3 years by default) · 🔴 missing · not required.
- **Add product** form that validates H-codes and pictograms and shows clear error messages.
- **Fill in from a safety data sheet:** upload the PDF and the form is filled in — product name,
  supplier, H-phrases (section 2 only), pictogram codes, revision date and language — for you to check and save.
- **Edit and delete** products (delete asks for confirmation and also removes the product's data sheets).
- **Manage data sheets** on the edit page: correct the revision date or language, remove one added by mistake,
  or add a newer version from the supplier — older versions are kept as history; the newest decides the status.
- **CLP pictogram check:** the pictograms are compared with the H-codes (incl. the CLP precedence rules).
  Missing or unexpected pictograms are shown in the form and the list; when a data sheet shows its
  pictograms only as images, they are suggested from the H-codes.
- **PostgreSQL storage**: products and data sheets survive a restart. Without a database configured, data is kept in memory.
- Example data in Development mode when the inventory is empty.

## Tech stack

| Area         | Choice                                                    |
|--------------|-----------------------------------------------------------|
| Language     | C# / .NET 10 (LTS)                                        |
| UI           | Blazor Web App (server rendering, interactive where needed) |
| Architecture | Clean Architecture: Domain → Application → Infrastructure / Web |
| Tests        | xUnit, including architecture tests                       |
| CI           | GitHub Actions: format check, build (warnings are errors), tests |

## Solution structure

```
src/
  KemiKoll.Domain/          Business rules: products, hazard statements, pictograms,
                            safety data sheets, companies. Depends on nothing.
  KemiKoll.Application/     Use cases (add product, list products, add data sheet)
                            and storage interfaces. Depends on Domain only.
  KemiKoll.Infrastructure/  Storage: PostgreSQL with EF Core (and in-memory versions),
                            database mapping and migrations.
  KemiKoll.Web/             Blazor UI and the composition root (Program.cs).
tests/
  KemiKoll.Domain.Tests/
  KemiKoll.Application.Tests/
docs/
  roadmap.md                Phases and versions.
  decisions/                Architecture decision records: why we chose what we chose.
```

## Getting started

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/aobeid1979/KemiKoll.git
cd KemiKoll
dotnet test
dotnet run --project src/KemiKoll.Web
```

Open the address shown in the terminal and go to **Products**.
Tip: `dotnet watch --project src/KemiKoll.Web` restarts automatically when you save a file.

### Local database (PostgreSQL in Docker)

Docker is optional: without a database connection, the app keeps data in memory
(lost on restart). With it, everything is stored in PostgreSQL.

Requirements: [Docker Desktop](https://www.docker.com/products/docker-desktop/).

1. Create your local settings file and choose a password in it:
   ```bash
   copy .env.example .env      # Windows
   cp .env.example .env        # macOS / Linux
   ```
2. Start the database:
   ```bash
   docker compose up -d
   ```
3. Check that it is running — the status should be **healthy**:
   ```bash
   docker compose ps
   ```

| Task | Command |
|------|---------|
| Stop (data is kept) | `docker compose down` |
| Start again | `docker compose up -d` |
| Delete all data | `docker compose down -v` |

The database is only reachable from your own computer (`localhost:5432`).
`.env` is ignored by Git, so your password is never committed.

### Database tables (EF Core migrations)

The tables are created and updated by EF Core *migrations* in
`src/KemiKoll.Infrastructure/Persistence/Migrations`. The `dotnet ef` tool is listed in
`.config/dotnet-tools.json`; install it once per clone:

```bash
dotnet tool restore
```

Create or update the tables in your local database (use the password from your `.env`):

```bash
dotnet ef database update --project src/KemiKoll.Infrastructure --connection "Host=localhost;Port=5432;Database=kemikoll;Username=kemikoll;Password=<your password>"
```

After changing the database mapping, add a migration and commit it:

```bash
dotnet ef migrations add <DescriptiveName> --project src/KemiKoll.Infrastructure --output-dir Persistence/Migrations
```

CI fails if the mapping was changed without a migration.

### Connect the app to the database

The connection string is stored with .NET **User Secrets**: on your own computer, outside the
project folder, never in Git. Set it once (use the password from your `.env`):

```bash
dotnet user-secrets set "ConnectionStrings:KemiKoll" "Host=localhost;Port=5432;Database=kemikoll;Username=kemikoll;Password=<your password>" --project src/KemiKoll.Web
```

When the app starts, the log shows **`Storage: PostgreSQL`** (or `Storage: in memory` if no
connection string is set). To go back to in-memory storage:

At startup the app checks the database and stops with one clear message if it cannot be used:
*Cannot reach the PostgreSQL database …* → start Docker and run `docker compose up -d`;
*The database is missing these migrations …* → run `dotnet ef database update` (see above).

```bash
dotnet user-secrets remove "ConnectionStrings:KemiKoll" --project src/KemiKoll.Web
```

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) for the branch, commit and pull request rules.
Changes are listed in the [CHANGELOG](CHANGELOG.md).

## License

Copyright (C) 2026 aobeid1979

KemiKoll is free software: you can redistribute it and/or modify it under the terms of the
**GNU Affero General Public License version 3** as published by the Free Software Foundation.
See [LICENSE](LICENSE).

In short: you may use, study, change and share the code. If you run a modified version
as a service for others, you must make your source code available to its users under the
same license. See [decision 0005](docs/decisions/0005-license-agpl.md) for why this license was chosen.

The GHS pictogram images are third-party files; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
