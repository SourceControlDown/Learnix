using Learnix.Application.Common.Specifications;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.Repositories;

public sealed class CourseRepository(ApplicationDbContext context) : ICourseRepository
{
    public Task<Course?> FirstOrDefaultAsync(Specification<Course> spec, CancellationToken ct = default)
        => SpecificationEvaluator<Course>.GetQuery(context.Courses, spec).FirstOrDefaultAsync(ct);

    public Task<bool> AnyAsync(Specification<Course> spec, CancellationToken ct = default)
        => SpecificationEvaluator<Course>.GetQuery(context.Courses, spec).AnyAsync(ct);

    public Task<int> CountAsync(Specification<Course> spec, CancellationToken ct = default)
        => SpecificationEvaluator<Course>.GetQuery(context.Courses, spec).CountAsync(ct);

    public async Task AddAsync(Course course, CancellationToken ct = default)
        => await context.Courses.AddAsync(course, ct);

    public void Update(Course course) => context.Courses.Update(course);

    public void Delete(Course course) => context.Courses.Remove(course);
}