using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Extensions;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using MediatR;

namespace Learnix.Application.Courses.Commands.UnarchiveCourse;

public sealed class UnarchiveCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UnarchiveCourseCommand, Result>
{
    public async Task<Result> Handle(UnarchiveCourseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, forUpdate: true), cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        if (!course.IsOwnerOrAdmin(currentUser))
            return Result.Fail(new ForbiddenError(CommonMessages.NotOwnerOfCourse));

        course.Unarchive();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
