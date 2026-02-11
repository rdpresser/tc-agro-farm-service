global using System.Diagnostics.CodeAnalysis;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Migrations;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Serilog;
global using TC.Agro.Farm.Application.Abstractions.Ports;
global using TC.Agro.Farm.Domain.Aggregates;
global using TC.Agro.Farm.Domain.Snapshots;
global using TC.Agro.SharedKernel.Application.Ports;
global using TC.Agro.SharedKernel.Domain.Aggregate;
global using TC.Agro.SharedKernel.Domain.Events;
global using TC.Agro.SharedKernel.Infrastructure.Database;
global using TC.Agro.SharedKernel.Infrastructure.Database.EfCore;
global using TC.Agro.SharedKernel.Infrastructure.Messaging.Outbox;
global using TC.Agro.SharedKernel.Infrastructure.UserClaims;
global using Wolverine.EntityFrameworkCore;
//**//
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TC.Agro.Farm.Unit.Tests")]
