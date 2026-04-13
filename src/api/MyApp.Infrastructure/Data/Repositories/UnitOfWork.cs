using MyApp.Domain.Interfaces;

namespace MyApp.Infrastructure.Data.Repositories;

public class UnitOfWork(AppDbContext context, IUserRepository users) : IUnitOfWork
{
    public IUserRepository Users { get; } = users;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
