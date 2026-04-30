using TC.Agro.Farm.Application.UseCases.CropTypes.GetById;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;

namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Read store interface for crop type suggestion queries (CQRS read side).
    /// </summary>
    public interface ICropTypeSuggestionReadStore
    {
        /// <summary>
        /// Gets a crop type suggestion by its identifier.
        /// </summary>
        Task<GetCropTypeByIdResponse?> GetByIdAsync(
            Guid id,
            bool includeInactive = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of crop type suggestions with optional filtering.
        /// </summary>
        Task<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)> ListAsync(
            ListCropTypesQuery query,
            CancellationToken cancellationToken = default);
    }
}
