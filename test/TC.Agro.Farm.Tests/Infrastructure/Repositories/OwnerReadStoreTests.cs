using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Application.UseCases.Owners.List;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.Farm.Infrastructure.Repositories;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Infrastructure.Repositories
{
    public class OwnerReadStoreTests
    {
        [Fact]
        public async Task ListOwnersAsync_WhenAdmin_ShouldReturnAllActiveOwners()
        {
            await using var dbContext = CreateDbContext();
            await SeedOwnersAsync(dbContext);

            var userContext = CreateUserContext(AppConstants.AdminRole, Guid.NewGuid());
            var sut = new OwnerReadStore(dbContext, userContext);

            var query = new ListOwnersQuery
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "name",
                SortDirection = "asc",
                Filter = ""
            };

            var (owners, totalCount) = await sut.ListOwnersAsync(query, TestContext.Current.CancellationToken);

            totalCount.ShouldBe(2);
            owners.Count.ShouldBe(2);
            owners.Select(x => x.Name).ShouldContain("Producer A");
            owners.Select(x => x.Name).ShouldContain("Producer B");
        }

        [Fact]
        public async Task ListOwnersAsync_WhenProducer_ShouldReturnOnlyCurrentOwner()
        {
            await using var dbContext = CreateDbContext();
            var producerAId = Guid.NewGuid();
            var producerBId = Guid.NewGuid();

            dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(producerAId, "Producer A", "producer.a@tcagro.com"));
            dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(producerBId, "Producer B", "producer.b@tcagro.com"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var userContext = CreateUserContext(AppConstants.ProducerRole, producerAId);
            var sut = new OwnerReadStore(dbContext, userContext);

            var query = new ListOwnersQuery { PageNumber = 1, PageSize = 10 };
            var (owners, totalCount) = await sut.ListOwnersAsync(query, TestContext.Current.CancellationToken);

            totalCount.ShouldBe(1);
            owners.Count.ShouldBe(1);
            owners[0].Id.ShouldBe(producerAId);
            owners[0].Name.ShouldBe("Producer A");
        }

        [Fact]
        public async Task ListOwnersAsync_WhenUserRole_ShouldReturnEmpty()
        {
            await using var dbContext = CreateDbContext();
            await SeedOwnersAsync(dbContext);

            var userContext = CreateUserContext(AppConstants.UserRole, Guid.NewGuid());
            var sut = new OwnerReadStore(dbContext, userContext);

            var query = new ListOwnersQuery { PageNumber = 1, PageSize = 10 };
            var (owners, totalCount) = await sut.ListOwnersAsync(query, TestContext.Current.CancellationToken);

            totalCount.ShouldBe(0);
            owners.Count.ShouldBe(0);
        }

        private static ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"owner-read-store-tests-{Guid.NewGuid()}")
                .Options;

            return new ApplicationDbContext(options);
        }

        private static async Task SeedOwnersAsync(ApplicationDbContext dbContext)
        {
            dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(Guid.NewGuid(), "Producer A", "producer.a@tcagro.com"));
            dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(Guid.NewGuid(), "Producer B", "producer.b@tcagro.com"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        private static IUserContext CreateUserContext(string role, Guid userId)
        {
            var userContext = A.Fake<IUserContext>();
            A.CallTo(() => userContext.Role).Returns(role);
            A.CallTo(() => userContext.IsAdmin)
                .Returns(string.Equals(role, AppConstants.AdminRole, StringComparison.OrdinalIgnoreCase));
            A.CallTo(() => userContext.Id).Returns(userId);
            A.CallTo(() => userContext.Name).Returns("Test User");
            A.CallTo(() => userContext.Email).Returns("test.user@tcagro.com");
            A.CallTo(() => userContext.Username).Returns("test.user");
            A.CallTo(() => userContext.IsAuthenticated).Returns(true);
            return userContext;
        }
    }
}
