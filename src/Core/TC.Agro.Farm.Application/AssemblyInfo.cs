global using Ardalis.Result;
global using FastEndpoints;
global using FluentValidation;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using System.Diagnostics.CodeAnalysis;
global using TC.Agro.Contracts.Events;
global using TC.Agro.Contracts.Events.Farm;
global using TC.Agro.Farm.Application.Abstractions;
global using TC.Agro.Farm.Application.Abstractions.Mappers;
global using TC.Agro.Farm.Application.Abstractions.Ports;
global using TC.Agro.Farm.Domain.Aggregates;
global using TC.Agro.Farm.Domain.ValueObjects;
global using TC.Agro.SharedKernel.Application.Commands;
global using TC.Agro.SharedKernel.Application.Handlers;
global using TC.Agro.SharedKernel.Application.Ports;
global using TC.Agro.SharedKernel.Application.Queries;
global using TC.Agro.SharedKernel.Domain.Aggregate;
global using TC.Agro.SharedKernel.Domain.Events;
global using TC.Agro.SharedKernel.Extensions;
global using TC.Agro.SharedKernel.Infrastructure.Authentication;
global using TC.Agro.SharedKernel.Infrastructure.Messaging;
global using TC.Agro.SharedKernel.Infrastructure.UserClaims;

// Domain Event aliases for cleaner code in mappers
global using PropertyCreatedDomainEvent = TC.Agro.Farm.Domain.Aggregates.PropertyAggregate.PropertyCreatedDomainEvent;
global using PropertyUpdatedDomainEvent = TC.Agro.Farm.Domain.Aggregates.PropertyAggregate.PropertyUpdatedDomainEvent;
global using PlotCreatedDomainEvent = TC.Agro.Farm.Domain.Aggregates.PlotAggregate.PlotCreatedDomainEvent;
global using SensorRegisteredDomainEvent = TC.Agro.Farm.Domain.Aggregates.SensorAggregate.SensorRegisteredDomainEvent;
//**//
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("TC.Agro.Farm.Unit.Tests")]
