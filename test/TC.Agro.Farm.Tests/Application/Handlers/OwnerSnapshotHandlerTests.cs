using FakeItEasy;
using TC.Agro.Contracts.Events.Identity;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.MessageBrokerHandlers;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.Messaging;

namespace TC.Agro.Farm.Tests.Application.Handlers
{
    public class OwnerSnapshotHandlerTests
    {
        private readonly IOwnerSnapshotStore _store;
        private readonly IUnitOfWork _unitOfWork;
        private readonly OwnerSnapshotHandler _handler;

        public OwnerSnapshotHandlerTests()
        {
            _store = A.Fake<IOwnerSnapshotStore>();
            _unitOfWork = A.Fake<IUnitOfWork>();
            _handler = new OwnerSnapshotHandler(_store, _unitOfWork);
        }

        private static EventContext<T> CreateEvent<T>(T eventData, Guid aggregateId) where T : class
        {
            return EventContext<T>.CreateBasic<PropertyAggregate>(eventData, aggregateId);
        }

        [Fact]
        public async Task HandleAsync_UserCreatedWithProducerRole_ShouldAddSnapshotAndSave()
        {
            var ownerId = Guid.NewGuid();
            var eventData = new UserCreatedIntegrationEvent(
                OwnerId: ownerId,
                Name: "Producer One",
                Email: "producer.one@tcagro.com",
                Username: "producer.one",
                Role: "Producer",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData, ownerId), TestContext.Current.CancellationToken);

            A.CallTo(() => _store.AddAsync(
                A<OwnerSnapshot>.That.Matches(s =>
                    s.Id == ownerId &&
                    s.Name == "Producer One" &&
                    s.Email == "producer.one@tcagro.com" &&
                    s.IsActive),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task HandleAsync_UserCreatedWithNonProducerRole_ShouldNotAddSnapshot()
        {
            var ownerId = Guid.NewGuid();
            var eventData = new UserCreatedIntegrationEvent(
                OwnerId: ownerId,
                Name: "Admin User",
                Email: "admin@tcagro.com",
                Username: "admin.user",
                Role: "Admin",
                OccurredOn: DateTimeOffset.UtcNow);

            await _handler.HandleAsync(CreateEvent(eventData, ownerId), TestContext.Current.CancellationToken);

            A.CallTo(() => _store.AddAsync(A<OwnerSnapshot>._, A<CancellationToken>._))
                .MustNotHaveHappened();

            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustNotHaveHappened();
        }
    }
}
