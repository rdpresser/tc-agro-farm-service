# MIGRATION-001: Crop Cycle and Catalog Refactor Plan

This document tracks the implementation phases agreed for the farm-service crop catalog and crop cycle refactor.

## Current Snapshot

As of 2026-03-11, the repository has already moved beyond the original baseline.

Implemented in code:

- Suggested image metadata on catalog and suggestions
- Tenant-scoped crop catalog (`owner_id` nullable)
- Catalog-first crop-type reads
- Crop cycles and immutable crop cycle events
- Property location blocking when active crop cycles exist

Still pending:

- Plot simplification and backfill migration
- Dedicated frontend/options parity work
- Cross-service crop-cycle analytics consumption

## Phase Table

| Phase   | Status | Scope                                                                                                                                    | Evidence                                                                                                                                                    |
| ------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Phase 0 | Done   | Add `crop_type_catalog_id` and `selected_crop_type_suggestion_id` to `plots` as the consolidated baseline.                               | `20260310205104_InitialMigration.cs`                                                                                                                        |
| Phase A | Done   | Add `suggested_image` to catalog and suggestions. Remove `COMMON_CROP_TYPES` from the crop type value object.                            | `20260311011332_AddSuggestedImageToCropTypes.cs`, `CropType.cs`, `CropTypeCatalogAggregate.cs`, `CropTypeSuggestionAggregate.cs`                            |
| Phase B | Done   | Introduce tenant-scoped catalog entries and switch crop-type list/get reads to catalog-first behavior with optional suggestion overlays. | `20260311023038_AddCropCatalogTenantScopeAndCropCycles.cs`, `CropTypeCatalogReadStore.cs`, `ListCropTypesQueryHandler.cs`, `GetCropTypeByIdQueryHandler.cs` |
| Phase C | Done   | Introduce `crop_cycles` and `crop_cycle_events`, plus start/transition/complete use cases and endpoints.                                 | `CropCycleAggregate.cs`, `CropCycleEventAggregate.cs`, `CropCycles/*`, `Endpoints/CropCycles/*`                                                             |
| Phase D | Next   | Backfill plot lifecycle columns into the first crop cycle per plot, then remove lifecycle and crop reference columns from `plots`.       | Target model preserved in `docs/domain/er-proposed-farm-service.md`                                                                                         |
| Phase E | Done   | Block property location changes when active crop cycles exist and trigger suggestion regeneration when location changes are allowed.     | `UpdatePropertyCommandHandler.cs`                                                                                                                           |
| Phase F | Next   | Remove frontend dependency on static crop lists and expose the final backend options contract expected by the UI.                        | Frontend parity work still pending                                                                                                                          |

## Relevant Files

### Domain and Application

- `src/Core/TC.Agro.Farm.Domain/Aggregates/CropTypeCatalogAggregate.cs`
- `src/Core/TC.Agro.Farm.Domain/Aggregates/CropTypeSuggestionAggregate.cs`
- `src/Core/TC.Agro.Farm.Domain/Aggregates/CropCycleAggregate.cs`
- `src/Core/TC.Agro.Farm.Domain/Aggregates/CropCycleEventAggregate.cs`
- `src/Core/TC.Agro.Farm.Domain/ValueObjects/CropCycleStatus.cs`
- `src/Core/TC.Agro.Farm.Application/UseCases/CropTypes/*`
- `src/Core/TC.Agro.Farm.Application/UseCases/CropCycles/*`
- `src/Core/TC.Agro.Farm.Application/UseCases/Properties/Update/UpdatePropertyCommandHandler.cs`

### Infrastructure and Schema

- `src/Adapters/Outbound/TC.Agro.Farm.Infrastructure/Migrations/20260310205104_InitialMigration.cs`
- `src/Adapters/Outbound/TC.Agro.Farm.Infrastructure/Migrations/20260311011332_AddSuggestedImageToCropTypes.cs`
- `src/Adapters/Outbound/TC.Agro.Farm.Infrastructure/Migrations/20260311023038_AddCropCatalogTenantScopeAndCropCycles.cs`
- `src/Adapters/Outbound/TC.Agro.Farm.Infrastructure/Repositories/CropTypeCatalogReadStore.cs`
- `src/Adapters/Outbound/TC.Agro.Farm.Infrastructure/Repositories/CropCycleAggregateRepository.cs`

## Next Migration Step

Phase D is the next structural migration to execute.

### Target change

Move the following fields out of `plots` and store them only in `crop_cycles`:

- `crop_type_catalog_id`
- `selected_crop_type_suggestion_id`
- `planting_date`
- `expected_harvest_date`
- `irrigation_type`
- `additional_notes`

### Migration sequence

1. Create one initial `crop_cycle` row for each existing plot carrying the current lifecycle fields.
2. Preserve the currently selected catalog and optional suggestion reference in that first cycle.
3. Validate that every active plot has a matching initial cycle before dropping plot columns.
4. Drop transitional lifecycle and crop reference columns from `plots`.

## Open Follow-Ups

The following items are not hidden by this plan and still require explicit follow-up work:

- The public crop-type write API is still suggestion-oriented. If the frontend CRUD is expected to mutate durable catalog entries, a dedicated catalog write contract must be introduced or the existing contract must be realigned.
- There is no dedicated `/api/crop-types/options` endpoint yet. Current list/get endpoints already expose catalog and overlay metadata, but the final UI contract is still a follow-up.
- Crop-cycle lifecycle changes are not yet consumed by `analytics-worker`. That means there is no real crop-cycle cross-service queue flow to validate today.
- The tenant-aware unique index for catalog names should evolve toward a composite normalized-name plus `owner_id` rule when Phase D or a later hardening pass is executed.

## Verification Checklist

- `er-current-farm-service.md` imports cleanly into dbdiagram.io.
- `er-proposed-farm-service.md` imports cleanly into dbdiagram.io.
- The migration changelog reflects all applied migrations in chronological order.
- Crop-cycle unit and integration tests cover start, transition, complete, and location blocking.
- Remaining gaps are explicitly listed instead of being silently treated as done.
