using Bogus;
using TC.Agro.Farm.Application.UseCases.Sensors.ListAll;
using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Service.Endpoints.Sensors
{
    public sealed class ListSensorsEndpoint : BaseApiEndpoint<ListSensorsQuery, PaginatedResponse<ListSensorsResponse>>
    {
        private static readonly string[] SensorTypes = ["Temperature", "Humidity", "SoilMoisture", "Rainfall"];
        private static readonly string[] SensorStatuses = ["Active", "Inactive", "Maintenance"];

        public override void Configure()
        {
            Get("sensors");

            RequestBinder(new RequestBinder<ListSensorsQuery>(BindingSource.QueryParams));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            this.AddQueryCachingIfNotTesting();
            Description(
                x => x.Produces<PaginatedResponse<ListSensorsResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            var faker = new Faker();
            List<ListSensorsResponse> sensorList = [];
            for (int i = 0; i < 5; i++)
            {
                var propertyId = Guid.NewGuid();
                var plotId = Guid.NewGuid();

                sensorList.Add(new ListSensorsResponse(
                    Guid.NewGuid(),
                    plotId,
                    $"Talhão {faker.Random.AlphaNumeric(2).ToUpper()}",
                    propertyId,
                    faker.Company.CompanyName() + " Farm",
                    Guid.NewGuid(),
                    faker.Name.FullName(),
                    faker.PickRandom(SensorTypes),
                    faker.PickRandom(SensorStatuses),
                    $"Sensor #{faker.Random.Int(1, 100)}",
                    DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 365))));
            }

            var exampleResponse = new PaginatedResponse<ListSensorsResponse>(
                data: [.. sensorList],
                totalCount: 42,
                pageNumber: 1,
                pageSize: 5
            );

            Summary(s =>
            {
                s.Summary = "Get a paginated list of sensors.";
                s.Description = "Retrieves a paginated list of sensors with optional filtering by plot, type, or status.";
                s.ExampleRequest = new ListSensorsQuery
                {
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "installedAt",
                    SortDirection = "desc",
                    Filter = "",
                    PropertyId = null,
                    PlotId = null,
                    Type = null,
                    Status = null
                };
                s.ResponseExamples[200] = exampleResponse;
                s.Responses[200] = "Returned when the sensor list is successfully retrieved.";
                s.Responses[400] = "Returned when the request contains invalid parameters.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
            });
        }

        public override async Task HandleAsync(ListSensorsQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}

