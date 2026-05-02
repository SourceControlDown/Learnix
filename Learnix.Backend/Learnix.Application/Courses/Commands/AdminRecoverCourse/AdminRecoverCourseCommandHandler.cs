using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Courses.Commands.AdminRecoverCourse;

internal sealed class AdminRecoverCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdminRecoverCourseCommand, Result>
{
    public async Task<Result> Handle(AdminRecoverCourseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only admins can recover courses."));

        var course = await courseRepository.FirstOrDefaultAsync(
            new AdminCourseByIdSpecification(request.CourseId, forUpdate: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course {request.CourseId} not found."));

        if (!course.IsDeleted)
            return Result.Fail(new ConflictError("Course is not deleted."));

        course.Recover();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
