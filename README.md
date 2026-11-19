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

> **Status:** early development — version **0.3.0**. See the [roadmap](docs/roadmap.md).
>
> KemiKoll is a support tool. It does not replace the employer's legal responsibility
> or professional advice.

## What works today

- **Product list** with name, supplier, hazard statements (H-phrases) and the official
  GHS pictograms.
- **Data sheet status** for every product:
  🟢 up to date · 🟠 due for review (older than 3 years by default) · 🔴 missing · not required.
- **Add product** form that validates H-codes and pictograms and shows clear error messages.
- Example data in Development mode. Data is kept in memory for now and is lost on restart.

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
  KemiKoll.Infrastructure/  Storage implementations (in memory today, database next).
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
