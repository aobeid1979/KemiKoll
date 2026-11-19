# 1. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-11-11

## Context

KemiKoll is built step by step, one small pull request at a time. Months later we need
to know not only *what* the code does but *why* it was designed that way.

## Decision

Every significant technical or product decision is written down as a short Markdown file
in `docs/decisions/`, numbered in order (`0001-…`, `0002-…`), with the sections
Context, Decision and Consequences. An accepted record is not rewritten; a new record
replaces it and says so.

## Consequences

- New contributors, customers and investors can follow the reasoning behind the design.
- Writing a record takes a few minutes per decision.
