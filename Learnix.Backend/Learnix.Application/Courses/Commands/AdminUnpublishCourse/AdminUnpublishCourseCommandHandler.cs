using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Courses.Commands.AdminUnpublishCourse;

internal sealed class AdminUnpublishCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdminUnpublishCourseCommand, Result>
{
    public async Task<Result> Handle(AdminUnpublishCourseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only admins can force-unpublish courses."));

        var course = await courseRepository.FirstOrDefaultAsync(
            new AdminCourseByIdSpecification(request.CourseId, forUpdate: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course {request.CourseId} not found."));

        if (course.Status != CourseStatus.Published)
            return Result.Fail(new ConflictError("Course is not published."));

        course.AdminUnpublish();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
