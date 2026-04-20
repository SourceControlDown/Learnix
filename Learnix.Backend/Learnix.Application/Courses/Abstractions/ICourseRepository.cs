using Learnix.Application.Common.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Abstractions;

public interface ICourseRepository
{
    Task<Course?> FirstOrDefaultAsync(Specification<Course> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(Specification<Course> spec, CancellationToken ct = default);
    Task<int> CountAsync(Specification<Course> spec, CancellationToken ct = default);
    Task AddAsync(Course course, CancellationToken ct = default);
    void Update(Course course);
    void Delete(Course course);
}
