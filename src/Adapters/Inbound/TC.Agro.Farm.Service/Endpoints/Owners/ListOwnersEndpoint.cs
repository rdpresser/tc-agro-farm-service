using Bogus;
using TC.Agro.Farm.Application.UseCases.Owners.List;
using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Service.Endpoints.Owners
{
    public sealed class ListOwnersEndpoint : BaseApiEndpoint<ListOwnersQuery, PaginatedResponse<ListOwnersResponse>>
    {
        public override void Configure()
        {
            Get("owners");

            RequestBinder(new RequestBinder<ListOwnersQuery>(BindingSource.QueryParams));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            this.AddQueryCachingIfNotTesting();
            Description(
                x => x.Produces<PaginatedResponse<ListOwnersResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            var faker = new Faker();
            List<ListOwnersResponse> ownerList = [];
            for (int i = 0; i < 5; i++)
            {
                ownerList.Add(new ListOwnersResponse(
                    Guid.NewGuid(),
                    faker.Name.FullName(),
                    faker.Internet.Email(),
                    true,
                    DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 365)),
                    faker.Random.Bool() ? DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(0, 30)) : null));
            }

            var exampleResponse = new PaginatedResponse<ListOwnersResponse>(
                data: [.. ownerList],
                totalCount: 42,
                pageNumber: 1,
                pageSize: 5
            );

            Summary(s =>
            {
                s.Summary = "Get a paginated list of active owners.";
                s.Description = "Retrieves active owner snapshots synchronized from Identity with role-based visibility.";
                s.ExampleRequest = new ListOwnersQuery
                {
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "createdat",
                    SortDirection = "desc",
                    Filter = ""
                };
                s.ResponseExamples[200] = exampleResponse;
                s.Responses[200] = "Returned when the owner list is successfully retrieved.";
                s.Responses[400] = "Returned when the request contains invalid parameters.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
            });
        }

        public override async Task HandleAsync(ListOwnersQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}

