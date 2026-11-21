# Roadmap

Each version ends with a git tag. Each item becomes one pull request.
The order puts something visible on screen early, then makes it real step by step.

## ✅ Phase 0 — Project setup
- [x] Folder structure, base configuration, solution and projects (#1–#3)
- [x] Test projects with architecture tests (#4)
- [x] CI on GitHub Actions and branch protection (#5)
- [x] Shared build and quality rules (#6)

## ✅ v0.2.0 — Domain model
- [x] Hazard statements (H-phrases) (#7)
- [x] Chemical products (#8)
- [x] Safety data sheets and review policy (#9)
- [x] GHS pictograms (#10)
- [x] Companies, organisation numbers and workplaces (#11)

## ✅ v0.3.0 — First screens
- [x] Product use cases with in-memory storage (#12)
- [x] Product list page (#13)
- [x] Add product form (#14)
- [x] Data sheet status in the list (#15)
- [x] Official GHS pictogram images (#16)

## ⏭️ v0.4.0 — Database
Data survives a restart. The screens do not change.
- [x] Local PostgreSQL database for development (Docker)
- [x] EF Core: database context and table mappings for products and data sheets
- [x] Database migrations (#20)
- [x] Use the database instead of in-memory storage

## v0.5.0 — PDF upload and extraction
The main feature: upload a safety data sheet, and the product is filled in.
- [x] Upload a PDF on the "Add product" page
- [ ] Store the PDF file itself, so it can be opened later
- [x] Extract text from the PDF (#22)
- [x] Find H-phrases, pictogram codes, revision date, product name and supplier with fixed rules ([decision 0006](decisions/0006-rule-based-extraction-first.md))
- [ ] Optional later: AI for the fields the rules cannot find
- [x] User checks and confirms the result in the existing form
- [ ] Tests with real, public safety data sheets

## v0.6.0 — Working with the inventory
- [ ] Edit and delete products
- [ ] Search and filter the list
- [ ] Export the chemical inventory to PDF and Excel
- [ ] Storage location (workplace) per product

## v0.7.0 — Companies and accounts
- [ ] Sign in; one company with several users
- [ ] Company settings, including its own review age
- [ ] Each company sees only its own data

## v0.8.0 — Risk assessment and reminders
- [ ] Suggested risk assessment per product, editable by the user
- [ ] Reminders for data sheets that are due for review or missing

## v1.0.0 — Pilot
- [ ] Hosting
- [ ] Free pilot with three real workshops
- [ ] Feedback round and pricing
