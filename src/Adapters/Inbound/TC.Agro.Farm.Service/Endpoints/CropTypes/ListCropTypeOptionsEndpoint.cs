using TC.Agro.Farm.Application.UseCases.CropTypes.Options;

namespace TC.Agro.Farm.Service.Endpoints.CropTypes
{
    public sealed class ListCropTypeOptionsEndpoint : BaseApiEndpoint<ListCropTypeOptionsQuery, IReadOnlyList<CropTypeOptionResponse>>
    {
        public override void Configure()
        {
            Get("crop-types/options");

            RequestBinder(new RequestBinder<ListCropTypeOptionsQuery>(BindingSource.QueryParams));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            this.AddQueryCachingIfNotTesting();
            Description(
                x => x.Produces<IReadOnlyList<CropTypeOptionResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Get crop type options for selectors.";
                s.Description = "Retrieves crop type options optimized for frontend selectors, including optional property overlays and references.";
                s.ExampleRequest = new ListCropTypeOptionsQuery
                {
                    PropertyId = Guid.NewGuid(),
                    IncludeStale = false,
                    IncludeInactive = false,
                    Limit = 200
                };
                s.Responses[200] = "Returned when crop type options are successfully retrieved.";
                s.Responses[400] = "Returned when the request contains invalid parameters.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
            });
        }

        public override async Task HandleAsync(ListCropTypeOptionsQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
