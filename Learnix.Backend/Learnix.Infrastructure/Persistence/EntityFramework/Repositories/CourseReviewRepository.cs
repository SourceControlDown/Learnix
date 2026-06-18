using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class CourseReviewRepository(ApplicationDbContext context)
    : RepositoryBase<CourseReview>(context), ICourseReviewRepository
{
}
