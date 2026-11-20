# Contributing to KemiKoll

These rules keep the history clean and make it possible to return to any step.

## Workflow: one step = one branch = one pull request

1. Start from an up-to-date `main`:
   ```bash
   git checkout main
   git pull
   ```
2. Create a branch:
   - `feature/<nn>-<short-name>` for new functionality, e.g. `feature/16-database`
   - `fix/<short-name>` for bug fixes
   - `docs/<short-name>` for documentation only
3. Commit in small, focused steps (see the commit rules below).
4. Push the branch and open a **pull request** to `main`.
5. **CI must be green**: formatting, build (warnings are errors) and tests.
   `main` is protected: merging is blocked until CI passes and the branch is up to date.
6. Merge with **Squash and merge**, so each step becomes one commit on `main`.
   The branch is deleted automatically.
7. At the end of a phase, tag a version (see [the roadmap](docs/roadmap.md)):
   ```bash
   git checkout main
   git pull
   git tag -a v0.4.0 -m "v0.4.0 - <what the version contains>"
   git push origin v0.4.0
   ```

Before running the app, check which branch you are on: `git branch --show-current`.

## Commit messages — Conventional Commits

```
<type>: <short summary in imperative mood>
```

| Type       | Use for                                 |
|------------|-----------------------------------------|
| `feat`     | New functionality                       |
| `fix`      | Bug fix                                 |
| `docs`     | Documentation only                      |
| `test`     | Adding or changing tests                |
| `refactor` | Code change that neither fixes nor adds |
| `chore`    | Build, tooling, files, dependencies     |
| `ci`       | CI configuration                        |

Examples: `feat: add PDF upload`, `test: cover hazard statement parsing`.

## Code rules

- **Dependency rule:** Domain depends on nothing; Application depends on Domain only.
  Architecture tests enforce this.
- **Warnings fail the build.** Nullable reference types and the .NET recommended analyzers are on
  (see `Directory.Build.props`).
- **Every public type and member in Domain, Application and Infrastructure has an XML comment** (`///`).
- **Comments explain *why*, not *what*.** Clear names explain the what.
- **Validation lives in the Domain and the use cases**, never only in a page.
- **New behaviour comes with tests.**
- **Database changes come with a migration.** If you change an entity or a mapping in
  `Infrastructure/Persistence`, run `dotnet ef migrations add <Name> --project src/KemiKoll.Infrastructure --output-dir Persistence/Migrations`
  and commit the generated files. Never edit a migration that is already merged; add a new one.
- Run `dotnet format KemiKoll.slnx` if CI reports a formatting error.

## Recording decisions

Significant technical or product decisions are written down in
[docs/decisions](docs/decisions/) — one short file per decision, numbered in order.
An accepted record is not rewritten; a new record replaces it.

## Going back to an earlier version

```bash
git tag                  # list versions
git checkout v0.2.0      # look at the code exactly as it was then
git checkout main        # back to the latest version
```

## License of contributions

KemiKoll is licensed under the GNU AGPL v3. By contributing, you agree that your
contribution is licensed under the same license.
