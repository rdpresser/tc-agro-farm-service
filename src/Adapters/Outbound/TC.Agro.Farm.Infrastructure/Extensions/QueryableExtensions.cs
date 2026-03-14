using System.Globalization;

namespace TC.Agro.Farm.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to apply sorting dynamically.
    /// Reduces code duplication across repositories.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Applies dynamic sorting to PropertyAggregate queries.
        /// </summary>
        public static IQueryable<PropertyAggregate> ApplySorting(
            this IQueryable<PropertyAggregate> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(p => p.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "name" => isAscending
                    ? query.OrderBy(p => p.Name.Value)
                    : query.OrderByDescending(p => p.Name.Value),
                "city" => isAscending
                    ? query.OrderBy(p => p.Location.City)
                    : query.OrderByDescending(p => p.Location.City),
                "state" => isAscending
                    ? query.OrderBy(p => p.Location.State)
                    : query.OrderByDescending(p => p.Location.State),
                "areahectares" => isAscending
                    ? query.OrderBy(p => p.AreaHectares.Hectares)
                    : query.OrderByDescending(p => p.AreaHectares.Hectares),
                "createdat" => isAscending
                    ? query.OrderBy(p => p.CreatedAt)
                    : query.OrderByDescending(p => p.CreatedAt),
                "ownername" => isAscending
                    ? query.OrderBy(p => p.Owner.Name)
                    : query.OrderByDescending(p => p.Owner.Name),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }

        /// <summary>
        /// Applies dynamic sorting to PlotAggregate queries.
        /// </summary>
        public static IQueryable<PlotAggregate> ApplySorting(
            this IQueryable<PlotAggregate> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(p => p.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "name" => isAscending
                    ? query.OrderBy(p => p.Name.Value)
                    : query.OrderByDescending(p => p.Name.Value),
                "croptype" => isAscending
                    ? query.OrderBy(p => p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value)
                    : query.OrderByDescending(p => p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value),
                "areahectares" => isAscending
                    ? query.OrderBy(p => p.AreaHectares.Hectares)
                    : query.OrderByDescending(p => p.AreaHectares.Hectares),
                "createdat" => isAscending
                    ? query.OrderBy(p => p.CreatedAt)
                    : query.OrderByDescending(p => p.CreatedAt),
                "propertyname" => isAscending
                    ? query.OrderBy(p => p.Property.Name.Value)
                    : query.OrderByDescending(p => p.Property.Name.Value),
                "sensorscount" => isAscending
                    ? query.OrderBy(p => p.Sensors.Count)
                    : query.OrderByDescending(p => p.Sensors.Count),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }

        /// <summary>
        /// Applies dynamic sorting to SensorAggregate queries.
        /// </summary>
        public static IQueryable<SensorAggregate> ApplySorting(
            this IQueryable<SensorAggregate> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(s => s.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "label" => isAscending
                    ? query.OrderBy(s => s.Label!.Value)
                    : query.OrderByDescending(s => s.Label!.Value),
                "type" => isAscending
                    ? query.OrderBy(s => s.Type.Value)
                    : query.OrderByDescending(s => s.Type.Value),
                "status" => isAscending
                    ? query.OrderBy(s => s.Status)
                    : query.OrderByDescending(s => s.Status),
                "installedat" => isAscending
                    ? query.OrderBy(s => s.InstalledAt)
                    : query.OrderByDescending(s => s.InstalledAt),
                "createdat" => isAscending
                    ? query.OrderBy(s => s.CreatedAt)
                    : query.OrderByDescending(s => s.CreatedAt),
                _ => query.OrderByDescending(s => s.CreatedAt)
            };
        }

        /// <summary>
        /// Applies dynamic sorting to OwnerSnapshot queries.
        /// </summary>
        public static IQueryable<OwnerSnapshot> ApplySorting(
            this IQueryable<OwnerSnapshot> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(o => o.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "name" => isAscending
                    ? query.OrderBy(o => o.Name)
                    : query.OrderByDescending(o => o.Name),
                "email" => isAscending
                    ? query.OrderBy(o => o.Email)
                    : query.OrderByDescending(o => o.Email),
                "isactive" => isAscending
                    ? query.OrderBy(o => o.IsActive)
                    : query.OrderByDescending(o => o.IsActive),
                "updatedat" => isAscending
                    ? query.OrderBy(o => o.UpdatedAt)
                    : query.OrderByDescending(o => o.UpdatedAt),
                "createdat" => isAscending
                    ? query.OrderBy(o => o.CreatedAt)
                    : query.OrderByDescending(o => o.CreatedAt),
                _ => query.OrderByDescending(o => o.CreatedAt)
            };
        }

        /// <summary>
        /// Applies dynamic sorting to CropTypeSuggestionAggregate queries.
        /// </summary>
        public static IQueryable<CropTypeSuggestionAggregate> ApplySorting(
            this IQueryable<CropTypeSuggestionAggregate> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(c => c.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "croptype" => isAscending
                    ? query.OrderBy(c => c.CropName.Value)
                    : query.OrderByDescending(c => c.CropName.Value),
                "propertyname" => isAscending
                    ? query.OrderBy(c => c.Property.Name.Value)
                    : query.OrderByDescending(c => c.Property.Name.Value),
                "confidence" => isAscending
                    ? query.OrderBy(c => c.ConfidenceScore)
                    : query.OrderByDescending(c => c.ConfidenceScore),
                "source" => isAscending
                    ? query.OrderBy(c => c.Source)
                    : query.OrderByDescending(c => c.Source),
                "isstale" => isAscending
                    ? query.OrderBy(c => c.IsStale)
                    : query.OrderByDescending(c => c.IsStale),
                "generatedat" => isAscending
                    ? query.OrderBy(c => c.GeneratedAt)
                    : query.OrderByDescending(c => c.GeneratedAt),
                "updatedat" => isAscending
                    ? query.OrderBy(c => c.UpdatedAt)
                    : query.OrderByDescending(c => c.UpdatedAt),
                "createdat" => isAscending
                    ? query.OrderBy(c => c.CreatedAt)
                    : query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };
        }

        /// <summary>
        /// Applies dynamic sorting to CropTypeCatalogAggregate queries.
        /// </summary>
        public static IQueryable<CropTypeCatalogAggregate> ApplySorting(
            this IQueryable<CropTypeCatalogAggregate> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(c => c.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "croptype" or "name" => isAscending
                    ? query.OrderBy(c => c.CropTypeName.Value)
                    : query.OrderByDescending(c => c.CropTypeName.Value),
                "ownername" => isAscending
                    ? query.OrderBy(c => c.Owner == null ? string.Empty : c.Owner.Name)
                    : query.OrderByDescending(c => c.Owner == null ? string.Empty : c.Owner.Name),
                "scientificname" => isAscending
                    ? query.OrderBy(c => c.ScientificName)
                    : query.OrderByDescending(c => c.ScientificName),
                "updatedat" => isAscending
                    ? query.OrderBy(c => c.UpdatedAt)
                    : query.OrderByDescending(c => c.UpdatedAt),
                "createdat" => isAscending
                    ? query.OrderBy(c => c.CreatedAt)
                    : query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };
        }

        /// <summary>
        /// Applies text search filter to PropertyAggregate queries.
        /// </summary>
        public static IQueryable<PropertyAggregate> ApplyTextFilter(
            this IQueryable<PropertyAggregate> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var pattern = $"%{filter}%";
            return query.Where(p =>
                EF.Functions.ILike(p.Name.Value, pattern) ||
                EF.Functions.ILike(p.Owner.Name, pattern) ||
                EF.Functions.ILike(p.Location.City, pattern) ||
                EF.Functions.ILike(p.Location.State, pattern) ||
                EF.Functions.ILike(p.Location.Country, pattern));
        }

        /// <summary>
        /// Applies text search filter to PlotAggregate queries.
        /// </summary>
        public static IQueryable<PlotAggregate> ApplyTextFilter(
            this IQueryable<PlotAggregate> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var pattern = $"%{filter.Trim()}%";
            return query.Where(p =>
                EF.Functions.ILike(p.Name.Value, pattern) ||
                EF.Functions.ILike(p.Property.Name.Value, pattern) ||
                EF.Functions.ILike(
                    p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value,
                    pattern));
        }

        /// <summary>
        /// Applies text search filter to OwnerSnapshot queries.
        /// Supports search by name, email, id, active status, and created/updated dates.
        /// </summary>
        public static IQueryable<OwnerSnapshot> ApplyTextFilter(
            this IQueryable<OwnerSnapshot> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var trimmedFilter = filter.Trim();
            var pattern = $"%{trimmedFilter}%";

            var hasGuid = Guid.TryParse(trimmedFilter, out var guidFilter);
            var hasBool = bool.TryParse(trimmedFilter, out var boolFilter);
            var hasDate = DateTimeOffset.TryParse(
                trimmedFilter,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dateFilter);

            var dayStart = hasDate ? dateFilter.Date : default;
            var dayEnd = hasDate ? dateFilter.Date.AddDays(1) : default;

            return query.Where(o =>
                EF.Functions.ILike(o.Name, pattern) ||
                EF.Functions.ILike(o.Email, pattern) ||
                (hasGuid && o.Id == guidFilter) ||
                (hasBool && o.IsActive == boolFilter) ||
                (hasDate && o.CreatedAt >= dayStart && o.CreatedAt < dayEnd) ||
                (hasDate && o.UpdatedAt.HasValue && o.UpdatedAt.Value >= dayStart && o.UpdatedAt.Value < dayEnd));
        }

        /// <summary>
        /// Applies text search filter to CropTypeSuggestionAggregate queries.
        /// </summary>
        public static IQueryable<CropTypeSuggestionAggregate> ApplyTextFilter(
            this IQueryable<CropTypeSuggestionAggregate> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var pattern = $"%{filter.Trim()}%";

            return query.Where(c =>
                EF.Functions.ILike(c.CropName.Value, pattern) ||
                EF.Functions.ILike(c.Property.Name.Value, pattern) ||
                EF.Functions.ILike(c.Owner.Name, pattern) ||
                EF.Functions.ILike(c.Source, pattern) ||
                (c.Notes != null && EF.Functions.ILike(c.Notes, pattern)) ||
                (c.SuggestedIrrigationType != null && EF.Functions.ILike(c.SuggestedIrrigationType, pattern)));
        }

        /// <summary>
        /// Applies text search filter to CropTypeCatalogAggregate queries.
        /// </summary>
        public static IQueryable<CropTypeCatalogAggregate> ApplyTextFilter(
            this IQueryable<CropTypeCatalogAggregate> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var pattern = $"%{filter.Trim()}%";

            return query.Where(c =>
                EF.Functions.ILike(c.CropTypeName.Value, pattern) ||
                (c.Description != null && EF.Functions.ILike(c.Description, pattern)) ||
                (c.ScientificName != null && EF.Functions.ILike(c.ScientificName, pattern)) ||
                (c.RecommendedIrrigationType != null && EF.Functions.ILike(c.RecommendedIrrigationType, pattern)) ||
                (c.Owner != null && EF.Functions.ILike(c.Owner.Name, pattern)));
        }

        /// <summary>
        /// Applies pagination to any queryable.
        /// </summary>
        public static IQueryable<T> ApplyPagination<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize)
        {
            return query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
    }
}
