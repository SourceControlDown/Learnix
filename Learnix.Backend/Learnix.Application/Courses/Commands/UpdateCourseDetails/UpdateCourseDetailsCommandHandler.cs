using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Commands.UpdateCourseDetails;

public sealed class UpdateCourseDetailsCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : CourseCommandHandler<UpdateCourseDetailsCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UpdateCourseDetailsCommand request, Course course, CancellationToken ct)
    {
        if (!await categoryRepository.AnyAsync(new CategoryByIdSpecification(request.CategoryId), ct))
            return Result.Fail(new NotFoundError(CommonMessages.CourseCategoryNotFound(request.CategoryId)));

        var normalizedTags = request.Tags
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .DistinctBy(t => t.ToLowerInvariant())
            .ToList();

        course.UpdateDetails(
            request.CategoryId,
            request.Title.Trim(),
            request.Description,
            request.Price,
            normalizedTags);

        course.SetCoverImage(request.CoverImageUrl);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
