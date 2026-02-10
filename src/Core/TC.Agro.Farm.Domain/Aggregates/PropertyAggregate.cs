using TC.Agro.Farm.Domain.Snapshots;

namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Property aggregate root - represents a farm property owned by a producer.
    /// </summary>
    public sealed class PropertyAggregate : BaseAggregateRoot
    {
        public Name Name { get; private set; } = default!;
        public Location Location { get; private set; } = default!;
        public Area AreaHectares { get; private set; } = default!;

        public Guid OwnerId { get; private set; }
        public OwnerSnapshot Owner { get; private set; } = default!;
        public ICollection<PlotAggregate> Plots { get; private set; } = [];

        // Private constructor for factories and ORM
        private PropertyAggregate(Guid id) : base(id) { }

        // Parameterless constructor for EF Core
        private PropertyAggregate() { }

        #region Factories

        public static Result<PropertyAggregate> Create(
            string name,
            string address,
            string city,
            string state,
            string country,
            double areaHectares,
            Guid ownerId,
            double? latitude = null,
            double? longitude = null)
        {
            var nameResult = ValueObjects.Name.Create(name);
            var locationResult = Location.Create(address, city, state, country, latitude, longitude);
            var areaResult = Area.Create(areaHectares);

            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(nameResult);
            errors.AddErrorsIfFailure(locationResult);
            errors.AddErrorsIfFailure(areaResult);
            errors.AddRange(ValidateOwnerId(ownerId));

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            return CreateAggregate(nameResult.Value, locationResult.Value, areaResult.Value, ownerId);
        }

        private static Result<PropertyAggregate> CreateAggregate(Name name, Location location, Area area, Guid ownerId)
        {
            var aggregate = new PropertyAggregate(Guid.NewGuid());
            var @event = new PropertyCreatedDomainEvent(
                aggregate.Id,
                name.Value,
                location.Address,
                location.City,
                location.State,
                location.Country,
                location.Latitude,
                location.Longitude,
                area.Hectares,
                ownerId,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Commands

        public Result Update(
            string name,
            string address,
            string city,
            string state,
            string country,
            double areaHectares,
            double? latitude = null,
            double? longitude = null)
        {
            var nameResult = ValueObjects.Name.Create(name);
            var locationResult = Location.Create(address, city, state, country, latitude, longitude);
            var areaResult = Area.Create(areaHectares);

            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(nameResult);
            errors.AddErrorsIfFailure(locationResult);
            errors.AddErrorsIfFailure(areaResult);

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var @event = new PropertyUpdatedDomainEvent(
                Id,
                nameResult.Value.Value,
                locationResult.Value.Address,
                locationResult.Value.City,
                locationResult.Value.State,
                locationResult.Value.Country,
                locationResult.Value.Latitude,
                locationResult.Value.Longitude,
                areaResult.Value.Hectares,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Deactivate()
        {
            if (!IsActive)
            {
                return Result.Invalid(FarmDomainErrors.PropertyAlreadyDeactivated);
            }

            var @event = new PropertyDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Activate()
        {
            if (IsActive)
            {
                return Result.Invalid(FarmDomainErrors.PropertyAlreadyActivated);
            }

            var @event = new PropertyActivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        #endregion

        #region Event Handlers

        public void Apply(PropertyCreatedDomainEvent @event)
        {
            SetId(@event.AggregateId);
            Name = ValueObjects.Name.FromDb(@event.Name).Value;
            Location = ValueObjects.Location.FromDb(
                @event.Address,
                @event.City,
                @event.State,
                @event.Country,
                @event.Latitude,
                @event.Longitude).Value;
            AreaHectares = Area.FromDb(@event.AreaHectares).Value;
            OwnerId = @event.OwnerId;
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public void Apply(PropertyUpdatedDomainEvent @event)
        {
            Name = ValueObjects.Name.FromDb(@event.Name).Value;
            Location = ValueObjects.Location.FromDb(
                @event.Address,
                @event.City,
                @event.State,
                @event.Country,
                @event.Latitude,
                @event.Longitude).Value;
            AreaHectares = Area.FromDb(@event.AreaHectares).Value;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(PropertyDeactivatedDomainEvent @event)
        {
            SetDeactivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(PropertyActivatedDomainEvent @event)
        {
            SetActivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case PropertyCreatedDomainEvent createdEvent:
                    Apply(createdEvent);
                    break;
                case PropertyUpdatedDomainEvent updatedEvent:
                    Apply(updatedEvent);
                    break;
                case PropertyDeactivatedDomainEvent deactivatedEvent:
                    Apply(deactivatedEvent);
                    break;
                case PropertyActivatedDomainEvent activatedEvent:
                    Apply(activatedEvent);
                    break;
            }
        }

        #endregion

        #region Validation Helpers

        private static IEnumerable<ValidationError> ValidateOwnerId(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
            {
                yield return FarmDomainErrors.OwnerIdRequired;
            }
        }

        #endregion

        #region Domain Events

        public record PropertyCreatedDomainEvent(
            Guid AggregateId,
            string Name,
            string Address,
            string City,
            string State,
            string Country,
            double? Latitude,
            double? Longitude,
            double AreaHectares,
            Guid OwnerId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PropertyUpdatedDomainEvent(
            Guid AggregateId,
            string Name,
            string Address,
            string City,
            string State,
            string Country,
            double? Latitude,
            double? Longitude,
            double AreaHectares,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PropertyDeactivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PropertyActivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
