using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => context.Categories.AnyAsync(c => c.Id == id, ct);

    public async Task<List<Category>> ListAllAsync(CancellationToken ct = default)
        => await context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => context.Categories.AnyAsync(c => c.Slug == slug, ct);

    public async Task AddAsync(Category category, CancellationToken ct = default)
        => await context.Categories.AddAsync(category, ct);
}
