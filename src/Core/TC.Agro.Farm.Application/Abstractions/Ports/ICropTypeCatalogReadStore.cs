using TC.Agro.Farm.Application.UseCases.CropTypes.GetById;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;

namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Read store interface for crop type catalog queries.
    /// Supports catalog-first reads with optional suggestion overlays.
    /// </summary>
    public interface ICropTypeCatalogReadStore
    {
        Task<GetCropTypeByIdResponse?> GetByIdAsync(
            Guid id,
            bool includeInactive = false,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)> ListAsync(
            ListCropTypesQuery query,
            CancellationToken cancellationToken = default);
    }
}
