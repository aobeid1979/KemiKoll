# 4. Build the first screens before the database

- **Status:** Accepted
- **Date:** 2026-11-16

## Context

After the domain model, the original plan was to add the database first and the screens
after it. That would mean many pull requests without anything visible, and no early
demo to show workshop owners.

## Decision

The first screens are built on **in-memory storage** (`InMemoryProductRepository`,
`InMemorySafetyDataSheetRepository`) behind the Application interfaces. The database
follows in v0.4.0 and replaces only those classes and their registration in `Program.cs`.
Example data is added in Development mode through the normal use cases.

## Consequences

- Visible results early; the UI can be shown and discussed before the database exists.
- Data is lost on every restart until v0.4.0.
- Proves the architecture: the database must fit behind the existing interfaces without
  changing the screens.
