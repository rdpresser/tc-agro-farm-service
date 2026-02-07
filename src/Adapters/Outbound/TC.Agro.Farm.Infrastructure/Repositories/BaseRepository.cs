namespace TC.Agro.Farm.Infrastructure.Repositories
{
    /// <summary>
    /// Base repository for Farm service aggregates.
    /// Extends SharedKernel's BaseRepository with ApplicationDbContext binding.
    /// </summary>
    public abstract class BaseRepository<TAggregate>
        : BaseRepository<TAggregate, ApplicationDbContext>
        where TAggregate : BaseAggregateRoot
    {
        protected BaseRepository(ApplicationDbContext dbContext) : base(dbContext) { }
    }
}
