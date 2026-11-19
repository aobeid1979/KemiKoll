# 5. License: GNU AGPL v3

- **Status:** Accepted
- **Date:** 2026-11-19

## Context

The repository is public. Without a license, nobody may legally use or contribute to the
code. The business plan is *open core*: the code is open, and the income comes from a
paid, hosted version of KemiKoll for workshops. Options considered: MIT and AGPL v3.

## Decision

KemiKoll is licensed under the **GNU Affero General Public License version 3** (`LICENSE`).

- Anyone may use, study, change and share the code.
- Anyone who runs a **modified** version as a service for others must make its source code
  available to those users under the same license.

MIT was rejected because it would let a competitor take the code, build a closed hosted
service on it and give nothing back.

## Consequences

- The paid hosted version is protected against closed copies.
- Contributions are licensed under AGPL v3 (stated in `CONTRIBUTING.md`).
- Some companies avoid AGPL code in their own products. This matters little, because
  customers use KemiKoll as a service instead of embedding the code.
- As the copyright holder, the owner can still offer the code under other terms later,
  for example a commercial license.
- This is not legal advice; for a commercial launch, check the license strategy with a lawyer.
