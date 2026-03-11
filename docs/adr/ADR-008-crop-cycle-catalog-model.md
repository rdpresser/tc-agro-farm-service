# ADR-008: Crop Cycle and Tenant Catalog Model

## Status

Accepted

## Context

The original farm model mixed three different concerns in the same place:

- `plots` represented the physical field identity.
- `plots` also stored the current crop lifecycle fields (`planting_date`, `expected_harvest_date`, `irrigation_type`, `additional_notes`).
- Crop knowledge was split between a global `crop_type_catalog`, AI/manual `crop_type_suggestions`, and a frontend fallback list (`COMMON_CROP_TYPES`).

That arrangement created four practical problems:

1. The same plot could only express one lifecycle snapshot and had no historical planting record.
2. Suggestions were valuable input, but they were too volatile to become the only source of truth for downstream flows.
3. The catalog could not be tenant-scoped, which prevented producer-specific crop options.
4. Property location changes could invalidate agronomic assumptions while active cycles were still in flight.

## Decision

### 1. Crop catalog is the source of truth

`crop_type_catalog` becomes the durable catalog used by the platform.

- `owner_id = NULL` means system-defined entry.
- `owner_id = <producer id>` means tenant-scoped entry.
- Frontend and backend selection flows should resolve the effective crop from the catalog first.

### 2. Suggestions remain volatile overlays

`crop_type_suggestions` remains useful, but it is not the primary truth source.

- Suggestions may come from AI or manual input.
- Suggestions can be marked stale and deactivated.
- Suggestions may optionally enrich a catalog-backed selection when a crop cycle or property-specific view needs metadata from the recommendation that influenced the choice.

### 3. Crop cycles become the historical planting record

A new `crop_cycles` aggregate stores the lifecycle of each planting execution on a plot.

- A plot can have many cycles over time.
- Only one active cycle is allowed per plot at a time.
- A cycle keeps references to the chosen catalog entry and the optional suggestion that influenced it.

### 4. Crop cycle events are immutable logs

A new `crop_cycle_events` table stores immutable transition history for auditability and future analytics integration.

- Start, status transition, and completion are logged as append-only events.
- Denormalized `plot_id`, `property_id`, and `owner_id` are kept on the event row for easier downstream inspection.

### 5. Plot is the physical footprint only

The target end-state is for `plots` to represent only the physical field context.

- Keep: ownership, location, geometry, and area.
- Move out: planting lifecycle dates, irrigation choice, additional agronomy notes, and effective crop choice.

This simplification is the agreed target even if the current codebase still carries some transitional crop fields on `plots`.

### 6. Property location changes are blocked while cycles are active

Changing the physical location of a property while active crop cycles exist can invalidate agronomic assumptions.

Decision:

- Reject property location changes when there is at least one active crop cycle for that property.
- When a location change is allowed, mark AI suggestions stale and trigger suggestion regeneration.

### 7. Frontend must stop relying on static crop constants

The frontend must stop using `COMMON_CROP_TYPES` as a long-term source of options.

Decision:

- Backend-provided catalog data becomes the canonical option source.
- Source badges and suggestion metadata remain visible to explain whether a value came from catalog or suggestion context.

## Consequences

### Positive

- Crop history becomes explicit and queryable.
- Tenant-specific catalogs become possible without duplicating the whole model.
- Suggestion metadata stays useful without replacing durable catalog truth.
- Property location updates are now safer for agronomic consistency.
- The model becomes easier to document in ER diagrams and easier to evolve toward analytics use cases.

### Negative

- The read model is more complex because it may need to combine catalog data and suggestion overlays.
- Transitional code must coexist until plot simplification is completed.
- Frontend contracts need parity work to stop depending on legacy static lists.

### Technical consequences

- `crop_type_catalog` needs tenant-aware uniqueness semantics. The long-term unique constraint should be composite on normalized name plus `owner_id`.
- Crop lifecycle changes are good candidates for transactional outbox publication once downstream analytics consumes those contracts.
- During the transition, public crop-type write endpoints must stay aligned with the final decision about whether the UI is editing catalog entries, suggestions, or both.

## Alternatives Considered

### Keep lifecycle fields directly on `plots`

Rejected because it cannot represent repeated planting history without overwriting previous state.

### Use `crop_type_suggestions` as the only crop source

Rejected because suggestions are intentionally volatile and may be regenerated, marked stale, or superseded.

### Keep a global catalog only

Rejected because producer-specific crop catalogs are a product requirement and a better fit for tenant ownership.

### Allow property location changes even with active cycles

Rejected because it weakens the integrity of agronomic data tied to active cycles and suggestion generation.

### Keep frontend fallback constants indefinitely

Rejected because it creates drift between UI behavior and backend truth.

## Implementation Notes

Implemented in code on 2026-03-11:

- Tenant-scoped `crop_type_catalog.owner_id`
- `suggested_image` on catalog and suggestions
- Catalog-first read model for list/get crop types
- `crop_cycles` and `crop_cycle_events`
- Property location blocking when active cycles exist

Still pending as follow-up:

- Plot simplification migration
- Dedicated frontend parity work and options contract cleanup
- Crop-cycle analytics consumer and contract publication when that integration is activated
