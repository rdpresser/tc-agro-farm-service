using System.Reflection;
using TC.Agro.Farm.Application.UseCases.Properties.Create;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Infrastructure;

namespace TC.Agro.Farm.Architecture.Tests;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(PropertyAggregate).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(CreatePropertyCommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;
    protected static readonly Assembly PresentationAssembly = typeof(Program).Assembly;
}
