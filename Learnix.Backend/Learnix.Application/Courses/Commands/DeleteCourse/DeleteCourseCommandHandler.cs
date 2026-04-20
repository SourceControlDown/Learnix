using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Courses.Commands.DeleteCourse;

public sealed class DeleteCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCourseCommand, Result>
{
    public async Task<Result> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("User is not authenticated."));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdForUpdateSpecification(request.CourseId), cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course '{request.CourseId}' was not found."));

        if (course.InstructorId != currentUser.UserId.Value && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("You are not allowed to delete this course."));

        // Raise event BEFORE repository.Delete — ChangeTracker dispatches from the entry
        // (even after SoftDeleteInterceptor flips state Deleted → Modified, the entry survives).
        course.MarkForDeletion();
        courseRepository.Delete(course);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
