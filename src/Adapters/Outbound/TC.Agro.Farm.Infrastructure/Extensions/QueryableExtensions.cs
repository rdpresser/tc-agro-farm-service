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
                    ? query.OrderBy(p => p.CropType.Value)
                    : query.OrderByDescending(p => p.CropType.Value),
                "areahectares" => isAscending
                    ? query.OrderBy(p => p.AreaHectares.Hectares)
                    : query.OrderByDescending(p => p.AreaHectares.Hectares),
                "createdat" => isAscending
                    ? query.OrderBy(p => p.CreatedAt)
                    : query.OrderByDescending(p => p.CreatedAt),
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

            var pattern = $"%{filter}%";
            return query.Where(p =>
                EF.Functions.ILike(p.Name.Value, pattern) ||
                EF.Functions.ILike(p.CropType.Value, pattern));
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
