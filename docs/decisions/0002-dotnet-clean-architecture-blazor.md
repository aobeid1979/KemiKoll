# 2. .NET 10, Clean Architecture and Blazor

- **Status:** Accepted
- **Date:** 2026-11-11

## Context

The founder is a .NET developer. The product must be maintainable by a small team,
easy to test, and able to change external services (database, PDF reading, AI provider)
without rewriting the business rules.

## Decision

- **.NET 10 (LTS)**, pinned in `global.json`.
- **Clean Architecture** with four projects. Dependencies point inward:
  - `Domain` — entities, value objects and rules; depends on nothing.
  - `Application` — use cases and interfaces such as `IProductRepository`; depends on Domain only.
  - `Infrastructure` — implementations of those interfaces.
  - `Web` — Blazor UI and `Program.cs`, the only place that connects interfaces to implementations.
- **Blazor Web App**: one language (C#) end to end. Pages are server-rendered;
  interactive rendering is used only where a page needs it (the "Add product" form).
- **xUnit**, including architecture tests that fail if a layer depends on the wrong project.
- **Strict build:** warnings are errors, recommended analyzers on, XML comments required
  on public code (`Directory.Build.props`).

## Consequences

- Business rules are unit-tested without a database, network or UI.
- External services can be replaced by changing one class and one line in `Program.cs`.
- More projects and ceremony than a single-project app.
