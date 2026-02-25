using Bogus;
using TC.Agro.Farm.Application.UseCases.Plots.ListAll;
using TC.Agro.Farm.Domain.ValueObjects;
using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Service.Endpoints.Plots
{
    public sealed class ListPlotsEndpoint : BaseApiEndpoint<ListPlotsQuery, PaginatedResponse<ListPlotsResponse>>
    {
        public override void Configure()
        {
            Get("plots");

            // Force FastEndpoints to bind from query parameters
            RequestBinder(new RequestBinder<ListPlotsQuery>(BindingSource.QueryParams));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            PreProcessor<QueryCachingPreProcessorBehavior<ListPlotsQuery, PaginatedResponse<ListPlotsResponse>>>();
            PostProcessor<QueryCachingPostProcessorBehavior<ListPlotsQuery, PaginatedResponse<ListPlotsResponse>>>();
            Description(
                x => x.Produces<PaginatedResponse<ListPlotsResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            var faker = new Faker();
            List<ListPlotsResponse> plotList = [];
            for (int i = 0; i < 5; i++)
            {
                plotList.Add(new ListPlotsResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    faker.Company.CompanyName() + " Farm",
                    $"Talhão {faker.Random.AlphaNumeric(2).ToUpper()}",
                    faker.PickRandom(CropType.CommonCropTypes),
                    faker.Random.Double(10, 100),
                    true,
                    faker.Random.Int(0, 5),
                    DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 365)),
                    DateTimeOffset.UtcNow.AddMonths(-2),
                    DateTimeOffset.UtcNow.AddMonths(6),
                    IrrigationType.CenterPivot,
                    null));
            }

            var exampleResponse = new PaginatedResponse<ListPlotsResponse>(
                data: [.. plotList],
                totalCount: 42,
                pageNumber: 1,
                pageSize: 5
            );

            Summary(s =>
            {
                s.Summary = "Get a paginated list of plots.";
                s.Description = "Retrieves a paginated list of plots with optional filtering by property or crop type.";
                s.ExampleRequest = new ListPlotsQuery
                {
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "name",
                    SortDirection = "asc",
                    Filter = "",
                    CropType = null
                };
                s.ResponseExamples[200] = exampleResponse;
                s.Responses[200] = "Returned when the plot list is successfully retrieved.";
                s.Responses[400] = "Returned when the request contains invalid parameters.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
            });
        }

        public override async Task HandleAsync(ListPlotsQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
