using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Infrastructure.Messaging;
using TC.Agro.Farm.Infrastructure.Repositories;
using TC.Agro.SharedKernel.Infrastructure;

namespace TC.Agro.Farm.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddFarmInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Property repositories
            services.AddScoped<IPropertyAggregateRepository, PropertyAggregateRepository>();
            services.AddScoped<IPropertyReadStore, PropertyReadStore>();

            // Plot repositories
            services.AddScoped<IPlotAggregateRepository, PlotAggregateRepository>();
            services.AddScoped<IPlotReadStore, PlotReadStore>();

            // Sensor repositories
            services.AddScoped<ISensorAggregateRepository, SensorAggregateRepository>();
            services.AddScoped<ISensorReadStore, SensorReadStore>();

            // -------------------------------
            // EF Core with Wolverine Integration
            // IMPORTANT: Use AddDbContextWithWolverineIntegration instead of AddDbContext
            // This enables the transactional outbox pattern with Wolverine
            // -------------------------------
            services.AddDbContextWithWolverineIntegration<ApplicationDbContext>((sp, opts) =>
            {
                var dbFactory = sp.GetRequiredService<DbConnectionFactory>();

                opts.UseNpgsql(dbFactory.ConnectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ApplicationDbContext.Schema);
                });

                opts.UseSnakeCaseNamingConvention();

                // Enable lazy loading proxies
                ////opts.UseLazyLoadingProxies();

                // Use Serilog for EF Core logging
                opts.LogTo(Log.Logger.Information, LogLevel.Information);

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    opts.EnableSensitiveDataLogging(true);
                    opts.EnableDetailedErrors();
                }

            });

            // Unit of Work (for simple handlers that don't need outbox)
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

            // Transactional Outbox (for handlers that publish integration events)
            // Uses Wolverine for atomic EF Core persistence + message publishing
            services.AddScoped<ITransactionalOutbox, FarmOutbox>();

            services.AddAgroInfrastructure(configuration);

            return services;
        }
    }
}
