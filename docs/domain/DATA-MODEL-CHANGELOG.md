# Farm Service Data Model Changelog

This changelog tracks the schema milestones relevant to the crop catalog and crop cycle refactor.

| Version | Date       | Migration File                                             | Status  | Description                                                                                                                                                                                                        | Breaking Change |
| ------- | ---------- | ---------------------------------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------- |
| 0.1.0   | 2026-03-10 | `20260310205104_InitialMigration.cs`                       | Applied | Introduced the baseline farm schema with `owner_snapshots`, `properties`, `crop_type_catalog`, `crop_type_suggestions`, `plots`, and `sensors`. `plots` carried both physical plot data and crop lifecycle fields. | No              |
| 0.2.0   | 2026-03-11 | `20260311011332_AddSuggestedImageToCropTypes.cs`           | Applied | Added `suggested_image` to `crop_type_catalog` and `crop_type_suggestions` so UI and AI-generated options can carry a compact visual marker.                                                                       | No              |
| 0.3.0   | 2026-03-11 | `20260311023038_AddCropCatalogTenantScopeAndCropCycles.cs` | Applied | Added nullable `owner_id` to `crop_type_catalog` for tenant scoping and introduced `crop_cycles` plus `crop_cycle_events` to start separating plot identity from planting history.                                 | No              |

## Pending Structural Change

The agreed target model still includes one major unfinished structural step:

- Move agronomy fields out of `plots` and keep them only in `crop_cycles`.
- Remove `plots.crop_type_catalog_id` and `plots.selected_crop_type_suggestion_id` after the data backfill is complete.

That work is tracked in `docs/migrations/MIGRATION-001-crop-cycle-catalog-refactor.md`.
