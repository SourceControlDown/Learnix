using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Courses.Commands.PublishCourse;

public sealed class PublishCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PublishCourseCommand, Result>
{
    public async Task<Result> Handle(PublishCourseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("User is not authenticated."));

        // Publish needs the full structure to verify invariants (ADR-040).
        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdWithStructureSpecification(request.CourseId, forUpdate: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course '{request.CourseId}' was not found."));

        if (course.InstructorId != currentUser.UserId.Value && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("You are not allowed to publish this course."));

        // Pre-validate invariants in handler for clean 409 ProblemDetails responses
        // (domain method throws — used as last-line defence only).
        if (string.IsNullOrWhiteSpace(course.CoverImageUrl))
            return Result.Fail(new ConflictError("Course cannot be published without a cover image."));

        if (course.Sections.Count == 0)
            return Result.Fail(new ConflictError("Course cannot be published without at least one section."));

        if (course.Sections.All(s => s.Lessons.Count == 0))
            return Result.Fail(new ConflictError("Course cannot be published without at least one lesson."));

        course.Publish();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
