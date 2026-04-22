using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Courses.Commands.UpdateCourseDetails;

public sealed class UpdateCourseDetailsCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCourseDetailsCommand, Result>
{
    public async Task<Result> Handle(UpdateCourseDetailsCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("User is not authenticated."));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdForUpdateSpecification(request.CourseId), cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course '{request.CourseId}' was not found."));

        if (course.InstructorId != currentUser.UserId.Value && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("You are not allowed to edit this course."));

        if (!await categoryRepository.AnyAsync(new CategoryByIdSpecification(request.CategoryId), cancellationToken))
            return Result.Fail(new NotFoundError($"Category '{request.CategoryId}' was not found."));

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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
