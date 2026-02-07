using System.Net;

using Bogus;

using FastEndpoints;

using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Application.UseCases.Plots.GetPlotList;
using TC.Agro.SharedKernel.Api.Endpoints;
using TC.Agro.SharedKernel.Application.Behaviors;
using TC.Agro.SharedKernel.Infrastructure;

namespace TC.Agro.Farm.Service.Endpoints.Plots
{
    public sealed class GetPlotListEndpoint : BaseApiEndpoint<GetPlotListQuery, IReadOnlyList<PlotListResponse>>
    {
        private static readonly string[] CropTypes = ["Soja", "Milho", "Café", "Cana-de-açúcar", "Algodão"];

        public override void Configure()
        {
            Get("plot");

            // Force FastEndpoints to bind from query parameters
            RequestBinder(new RequestBinder<GetPlotListQuery>(BindingSource.QueryParams));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            PreProcessor<QueryCachingPreProcessorBehavior<GetPlotListQuery, IReadOnlyList<PlotListResponse>>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetPlotListQuery, IReadOnlyList<PlotListResponse>>>();

            Description(
                x => x.Produces<IReadOnlyList<PlotListResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            var faker = new Faker();
            List<PlotListResponse> plotList = [];
            for (int i = 0; i < 5; i++)
            {
                plotList.Add(new PlotListResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    faker.Company.CompanyName() + " Farm",
                    $"Talhão {faker.Random.AlphaNumeric(2).ToUpper()}",
                    faker.PickRandom(CropTypes),
                    faker.Random.Double(10, 100),
                    true,
                    faker.Random.Int(0, 5),
                    DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 365))));
            }

            Summary(s =>
            {
                s.Summary = "Get a paginated list of plots.";
                s.Description = "Retrieves a paginated list of plots with optional filtering by property or crop type.";
                s.ExampleRequest = new GetPlotListQuery
                {
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "name",
                    SortDirection = "asc",
                    Filter = "",
                    PropertyId = null,
                    CropType = null
                };
                s.ResponseExamples[200] = plotList;
                s.Responses[200] = "Returned when the plot list is successfully retrieved.";
                s.Responses[400] = "Returned when the request contains invalid parameters.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
            });
        }

        public override async Task HandleAsync(GetPlotListQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
