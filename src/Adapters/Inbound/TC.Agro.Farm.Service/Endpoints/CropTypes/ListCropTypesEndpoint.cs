using TC.Agro.Farm.Application.UseCases.CropTypes.List;
using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Service.Endpoints.CropTypes
{
    public sealed class ListCropTypesEndpoint : BaseApiEndpoint<ListCropTypesQuery, PaginatedResponse<ListCropTypesResponse>>
    {
        public override void Configure()
        {
            Get("crop-types");

            RequestBinder(new RequestBinder<ListCropTypesQuery>(BindingSource.QueryParams));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            this.AddQueryCachingIfNotTesting();
            Description(
                x => x.Produces<PaginatedResponse<ListCropTypesResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Get crop type catalog entries.";
                s.Description = "Retrieves tenant-scoped crop type catalog entries with optional property-specific suggestion overlays.";
                s.ExampleRequest = new ListCropTypesQuery
                {
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "createdAt",
                    SortDirection = "desc",
                    PropertyId = Guid.NewGuid(),
                    IncludeStale = false,
                    IncludeInactive = false
                };
                s.Responses[200] = "Returned when crop type catalog entries are successfully retrieved.";
                s.Responses[400] = "Returned when the request contains invalid parameters.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
            });
        }

        public override async Task HandleAsync(ListCropTypesQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
