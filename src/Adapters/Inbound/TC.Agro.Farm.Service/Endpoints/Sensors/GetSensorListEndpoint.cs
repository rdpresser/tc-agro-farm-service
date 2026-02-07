using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Service.Endpoints.Sensors
{
    public sealed class GetSensorListEndpoint : BaseApiEndpoint<GetSensorListQuery, PaginatedResponse<SensorListResponse>>
    {
        private static readonly string[] SensorTypes = ["Temperature", "Humidity", "SoilMoisture", "Rainfall"];
        private static readonly string[] SensorStatuses = ["Active", "Inactive", "Maintenance"];

        public override void Configure()
        {
            Get("sensor");

            // Force FastEndpoints to bind from query parameters
            RequestBinder(new RequestBinder<GetSensorListQuery>(BindingSource.QueryParams));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            PreProcessor<QueryCachingPreProcessorBehavior<GetSensorListQuery, PaginatedResponse<SensorListResponse>>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetSensorListQuery, PaginatedResponse<SensorListResponse>>>();

            Description(
                x => x.Produces<PaginatedResponse<SensorListResponse>>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            var faker = new Faker();
            List<SensorListResponse> sensorList = [];
            for (int i = 0; i < 5; i++)
            {
                sensorList.Add(new SensorListResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    $"Talhão {faker.Random.AlphaNumeric(2).ToUpper()}",
                    Guid.NewGuid(),
                    faker.Company.CompanyName() + " Farm",
                    faker.PickRandom(SensorTypes),
                    faker.PickRandom(SensorStatuses),
                    $"Sensor #{faker.Random.Int(1, 100)}",
                    DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 365))));
            }

            var exampleResponse = new PaginatedResponse<SensorListResponse>(
                data: [.. sensorList],
                totalCount: 42,
                pageNumber: 1,
                pageSize: 5
            );

            Summary(s =>
            {
                s.Summary = "Get a paginated list of sensors.";
                s.Description = "Retrieves a paginated list of sensors with optional filtering by plot, property, type, or status.";
                s.ExampleRequest = new GetSensorListQuery
                {
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "installedAt",
                    SortDirection = "desc",
                    Filter = "",
                    PlotId = null,
                    PropertyId = null,
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

        public override async Task HandleAsync(GetSensorListQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
