using System.Net;

using Bogus;

using FastEndpoints;

using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Application.UseCases.Properties.CreateProperty;
using TC.Agro.SharedKernel.Api.Endpoints;
using TC.Agro.SharedKernel.Application.Behaviors;
using TC.Agro.SharedKernel.Infrastructure;

namespace TC.Agro.Farm.Service.Endpoints.Properties
{
    public sealed class CreatePropertyEndpoint : BaseApiEndpoint<CreatePropertyCommand, CreatePropertyResponse>
    {
        public override void Configure()
        {
            Post("property");
            PostProcessor<LoggingCommandPostProcessorBehavior<CreatePropertyCommand, CreatePropertyResponse>>();

            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<CreatePropertyResponse>(201)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Create a new property.";
                s.Description = "This endpoint allows producers or admins to register a new property (farm) in the system.";
                s.ExampleRequest = new CreatePropertyCommand(
                    "Fazenda Boa Vista",
                    "Estrada Rural Km 15",
                    "Ribeirão Preto",
                    "SP",
                    "Brazil",
                    250.5,
                    -21.1767,
                    -47.8208);
                s.ResponseExamples[201] = new CreatePropertyResponse(
                    Guid.NewGuid(),
                    "Fazenda Boa Vista",
                    "Estrada Rural Km 15",
                    "Ribeirão Preto",
                    "SP",
                    "Brazil",
                    -21.1767,
                    -47.8208,
                    250.5,
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow);
                s.Responses[201] = "Returned when the property is successfully created.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role to create properties.";
            });
        }

        public override async Task HandleAsync(CreatePropertyCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                string location = $"/api/property/{response.Value.Id}";
                object routeValues = new { id = response.Value.Id };
                await Send.CreatedAtAsync(location, routeValues, response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
