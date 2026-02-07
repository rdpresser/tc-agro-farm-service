using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Service.Endpoints.Properties
{
    public sealed class GetPropertyListEndpoint : BaseApiEndpoint<GetPropertyListQuery, PaginatedResponse<PropertyListResponse>>
    {
        public override void Configure()
        {
            Get("property");

            // Force FastEndpoints to bind from query parameters
            RequestBinder(new RequestBinder<GetPropertyListQuery>(BindingSource.QueryParams));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            PreProcessor<QueryCachingPreProcessorBehavior<GetPropertyListQuery, PaginatedResponse<PropertyListResponse>>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetPropertyListQuery, PaginatedResponse<PropertyListResponse>>>();

            Description(
                x => x.Produces<PaginatedResponse<PropertyListResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            var faker = new Faker();
            List<PropertyListResponse> propertyList = [];
            for (int i = 0; i < 5; i++)
            {
                propertyList.Add(new PropertyListResponse(
                    Guid.NewGuid(),
                    faker.Company.CompanyName() + " Farm",
                    faker.Address.City(),
                    faker.Address.StateAbbr(),
                    "Brazil",
                    faker.Random.Double(50, 500),
                    Guid.NewGuid(),
                    true,
                    faker.Random.Int(1, 10),
                    DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 365))));
            }

            var exampleResponse = new PaginatedResponse<PropertyListResponse>(
                data: [.. propertyList],
                totalCount: 42,
                pageNumber: 1,
                pageSize: 5
            );

            Summary(s =>
            {
                s.Summary = "Get a paginated list of properties.";
                s.Description = "Retrieves a paginated list of properties with optional filtering by owner.";
                s.ExampleRequest = new GetPropertyListQuery
                {
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "name",
                    SortDirection = "asc",
                    Filter = "",
                    OwnerId = null
                };
                s.ResponseExamples[200] = exampleResponse;
                s.Responses[200] = "Returned when the property list is successfully retrieved.";
                s.Responses[400] = "Returned when the request contains invalid parameters.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
            });
        }

        public override async Task HandleAsync(GetPropertyListQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
