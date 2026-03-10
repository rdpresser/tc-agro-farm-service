using TC.Agro.Farm.Application.UseCases.CropTypes.GetById;

namespace TC.Agro.Farm.Service.Endpoints.CropTypes
{
    public sealed class GetCropTypeByIdEndpoint : BaseApiEndpoint<GetCropTypeByIdQuery, GetCropTypeByIdResponse>
    {
        public override void Configure()
        {
            Get("crop-types/{id:guid}");

            RequestBinder(new RequestBinder<GetCropTypeByIdQuery>(BindingSource.RouteValues));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            this.AddQueryCachingIfNotTesting();
            Description(
                x => x.Produces<GetCropTypeByIdResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Get crop type suggestion details.";
                s.Description = "Retrieves details of a specific crop type suggestion by identifier.";
                s.ExampleRequest = new GetCropTypeByIdQuery { Id = Guid.NewGuid(), IncludeInactive = false };
                s.Responses[200] = "Returned when the crop type suggestion is found.";
                s.Responses[400] = "Returned when the request is invalid.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when no crop type suggestion is found with the given ID.";
            });
        }

        public override async Task HandleAsync(GetCropTypeByIdQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
