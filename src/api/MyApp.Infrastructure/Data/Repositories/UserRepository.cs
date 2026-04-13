using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;

namespace MyApp.Infrastructure.Data.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => await context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => await context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Users.ToListAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await context.Users.AddAsync(user, cancellationToken);

    public void Update(User user) => context.Users.Update(user);

    public void Delete(User user) => context.Users.Remove(user);
}
