using FakeItEasy;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.Caching.Service;
using Wolverine;
using FarmProgram = TC.Agro.Farm.Service.Program;

namespace TC.Agro.Farm.Tests.TestHelpers.Api;

public sealed class FarmApiWebApplicationFactory : WebApplicationFactory<FarmProgram>
{
    private readonly string _databaseName = $"farm-api-tests-{Guid.NewGuid():N}";

    public FarmApiWebApplicationFactory()
    {
        foreach (var (key, value) in TestConfiguration.Values)
        {
            Environment.SetEnvironmentVariable(ToEnvironmentVariableKey(key), value);
        }
    }

    public HttpClient CreateAuthenticatedClient(string role, Guid? userId = null, string? email = null)
    {
        var effectiveUserId = userId ?? Guid.NewGuid();
        var effectiveEmail = email ?? $"{effectiveUserId:N}@tcagro.test";

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add(TestAuthDefaults.RoleHeader, role);
        client.DefaultRequestHeaders.Add(TestAuthDefaults.UserIdHeader, effectiveUserId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthDefaults.EmailHeader, effectiveEmail);
        client.DefaultRequestHeaders.Add(TestAuthDefaults.NameHeader, "API Test User");
        client.DefaultRequestHeaders.Add(TestAuthDefaults.UsernameHeader, "api.test.user");

        return client;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public async Task SeedOwnerAsync(Guid ownerId, string? name = null, string? email = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(
            ownerId,
            name ?? "Producer API Test",
            email ?? $"{ownerId:N}@owner.test"));

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(TestConfiguration.Values);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            services.RemoveAll<ITransactionalOutbox>();
            services.AddScoped<ITransactionalOutbox>(sp =>
                new TestTransactionalOutbox(sp.GetRequiredService<ApplicationDbContext>()));

            services.RemoveAll<IMessageBus>();
            services.AddSingleton(_ => A.Fake<IMessageBus>());

            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthDefaults.Scheme;
                    options.DefaultChallengeScheme = TestAuthDefaults.Scheme;
                    options.DefaultScheme = TestAuthDefaults.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthDefaults.Scheme,
                    _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var key in TestConfiguration.Values.Keys)
        {
            Environment.SetEnvironmentVariable(ToEnvironmentVariableKey(key), null);
        }

        base.Dispose(disposing);
    }

    private static string ToEnvironmentVariableKey(string key)
        => key.Replace(":", "__", StringComparison.Ordinal);

    private static class TestConfiguration
    {
        public static readonly IReadOnlyDictionary<string, string?> Values = new Dictionary<string, string?>
        {
            ["Database:Postgres:Host"] = "localhost",
            ["Database:Postgres:Port"] = "5432",
            ["Database:Postgres:Database"] = "farm-tests",
            ["Database:Postgres:MaintenanceDatabase"] = "postgres",
            ["Database:Postgres:UserName"] = "postgres",
            ["Database:Postgres:Password"] = "postgres",
            ["Database:Postgres:Schema"] = "public",
            ["Database:Postgres:ConnectionTimeout"] = "15",
            ["Database:Postgres:MinPoolSize"] = "1",
            ["Database:Postgres:MaxPoolSize"] = "5",

            ["Cache:Redis:Host"] = "localhost",
            ["Cache:Redis:Port"] = "6379",
            ["Cache:Redis:Password"] = string.Empty,
            ["Cache:Redis:InstanceName"] = "farm-api-tests",

            ["Messaging:RabbitMQ:Host"] = "localhost",
            ["Messaging:RabbitMQ:Port"] = "5672",
            ["Messaging:RabbitMQ:ManagementPort"] = "15672",
            ["Messaging:RabbitMQ:VirtualHost"] = "/",
            ["Messaging:RabbitMQ:UserName"] = "guest",
            ["Messaging:RabbitMQ:Password"] = "guest",
            ["Messaging:RabbitMQ:Exchange"] = "farm.events",
            ["Messaging:RabbitMQ:AutoProvision"] = "false",
            ["Messaging:RabbitMQ:AutoPurgeOnStartup"] = "false",
            ["Messaging:RabbitMQ:UseQuorumQueues"] = "false",

            ["Telemetry:Grafana:Agent:Host"] = "localhost",
            ["Telemetry:Grafana:Agent:OtlpGrpcPort"] = "4317",
            ["Telemetry:Grafana:Agent:OtlpHttpPort"] = "4318",
            ["Telemetry:Grafana:Agent:MetricsPort"] = "12345",
            ["Telemetry:Grafana:Agent:Enabled"] = "false",
            ["Telemetry:Grafana:Otlp:Endpoint"] = "http://localhost:4318",
            ["Telemetry:Grafana:Otlp:Protocol"] = "http/protobuf",
            ["Telemetry:Grafana:Otlp:TimeoutSeconds"] = "5",

            ["Auth:Jwt:SecretKey"] = "0123456789abcdef0123456789abcdef",
            ["Auth:Jwt:Issuer"] = "tc-agro-tests",
            ["Auth:Jwt:Audience:0"] = "tc-agro-tests",
            ["Auth:Jwt:ExpirationInMinutes"] = "60"
        };
    }

    private static class TestAuthDefaults
    {
        public const string Scheme = "TestScheme";
        public const string RoleHeader = "X-Test-Role";
        public const string UserIdHeader = "X-Test-User-Id";
        public const string EmailHeader = "X-Test-Email";
        public const string NameHeader = "X-Test-Name";
        public const string UsernameHeader = "X-Test-Username";
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(TestAuthDefaults.RoleHeader, out var roleValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var role = roleValues.ToString();
            if (string.IsNullOrWhiteSpace(role))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing test role header."));
            }

            var userIdValue = Request.Headers.TryGetValue(TestAuthDefaults.UserIdHeader, out var userIdValues)
                ? userIdValues.ToString()
                : Guid.NewGuid().ToString();

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test user id header."));
            }

            var email = Request.Headers.TryGetValue(TestAuthDefaults.EmailHeader, out var emailValues)
                ? emailValues.ToString()
                : $"{userId:N}@tcagro.test";

            var name = Request.Headers.TryGetValue(TestAuthDefaults.NameHeader, out var nameValues)
                ? nameValues.ToString()
                : "API Test User";

            var username = Request.Headers.TryGetValue(TestAuthDefaults.UsernameHeader, out var usernameValues)
                ? usernameValues.ToString()
                : "api.test.user";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(ClaimTypes.Name, name),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim("role", role),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, TestAuthDefaults.Scheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestAuthDefaults.Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class TestTransactionalOutbox : ITransactionalOutbox
    {
        private readonly ApplicationDbContext _dbContext;

        public TestTransactionalOutbox(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public ValueTask EnqueueAsync<T>(T message, CancellationToken ct = default)
            => ValueTask.CompletedTask;

        public ValueTask EnqueueAsync<T>(IReadOnlyCollection<T> messages, CancellationToken ct = default)
            => ValueTask.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _dbContext.SaveChangesAsync(ct);
    }

    private sealed class NoOpCacheService : ICacheService
    {
        public NoOpCacheService()
        {
        }

        public Task<T?> GetAsync<T>(
            string key,
            TimeSpan? duration = null,
            TimeSpan? distributedCacheDuration = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<T?>(default);

        public Task<T?> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? duration = null,
            TimeSpan? distributedCacheDuration = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<T?>(default);

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? duration = null,
            TimeSpan? distributedCacheDuration = null,
            IReadOnlyCollection<string>? tags = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(
            string key,
            TimeSpan? duration = null,
            TimeSpan? distributedCacheDuration = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
