using FakeItEasy;
using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.TestHelpers;

internal static class TestUserContextFactory
{
    public static IUserContext CreateAdmin(Guid? userId = null, string email = "admin@tcagro.com")
        => Create(userId ?? Guid.NewGuid(), AppConstants.AdminRole, email, isAuthenticated: true);

    public static IUserContext CreateProducer(Guid? userId = null, string email = "producer@tcagro.com")
        => Create(userId ?? Guid.NewGuid(), AppConstants.ProducerRole, email, isAuthenticated: true);

    public static IUserContext CreateUser(Guid? userId = null, string email = "user@tcagro.com")
        => Create(userId ?? Guid.NewGuid(), AppConstants.UserRole, email, isAuthenticated: true);

    public static IUserContext CreateAnonymous()
        => Create(Guid.Empty, AppConstants.UnknownRole, "anonymous@tcagro.com", isAuthenticated: false);

    public static IUserContext Create(
        Guid userId,
        string role,
        string email,
        bool isAuthenticated,
        string? correlationId = "farm-tests-correlation-id")
    {
        var context = A.Fake<IUserContext>();

        A.CallTo(() => context.Id).Returns(userId);
        A.CallTo(() => context.Name).Returns("Test User");
        A.CallTo(() => context.Email).Returns(email);
        A.CallTo(() => context.Username).Returns("test.user");
        A.CallTo(() => context.Role).Returns(role);
        A.CallTo(() => context.CorrelationId).Returns(correlationId);
        A.CallTo(() => context.TenantId).Returns(null);
        A.CallTo(() => context.IsAuthenticated).Returns(isAuthenticated);
        A.CallTo(() => context.IsAdmin).Returns(string.Equals(role, AppConstants.AdminRole, StringComparison.OrdinalIgnoreCase));

        return context;
    }
}
