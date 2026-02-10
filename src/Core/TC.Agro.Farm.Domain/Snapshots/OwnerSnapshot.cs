using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Domain.Snapshots
{
    public class OwnerSnapshot
    {
        public Guid Id { get; private set; }      // Identity user id
        public string Name { get; private set; } = default!;
        public string Email { get; private set; } = default!;
        public bool IsActive { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }

        public ICollection<PropertyAggregate> Properties { get; private set; } = [];

        private OwnerSnapshot() { } // EF

        private OwnerSnapshot(Guid id, string name, string email, bool isActive, DateTimeOffset createdAt, DateTimeOffset? updatedAt)
        {
            Id = id;
            Name = name;
            Email = email;
            IsActive = isActive;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static OwnerSnapshot Create(Guid id, string name, string email)
        {
            var now = DateTimeOffset.UtcNow;
            return new OwnerSnapshot(id, name, email, true, now, null);
        }

        public static OwnerSnapshot Create(Guid id, string name, string email, DateTimeOffset createdAt)
        {
            return new OwnerSnapshot(id, name, email, true, createdAt, null);
        }

        public void Update(string name, string email, bool isActive)
        {
            Name = name;
            Email = email;
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Delete()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
