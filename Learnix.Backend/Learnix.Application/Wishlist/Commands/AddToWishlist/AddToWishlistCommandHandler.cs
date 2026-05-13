using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Wishlist.Commands.AddToWishlist;

public sealed class AddToWishlistCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddToWishlistCommand, Result>
{
    public async Task<Result> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        var userId = currentUser.UserId.Value;

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course {request.CourseId} not found."));

        if (course.Status != CourseStatus.Published)
            return Result.Fail(new ConflictError("Only published courses can be added to wishlist."));

        var alreadyEnrolled = await enrollmentRepository.AnyAsync(
            new EnrollmentByStudentAndCourseSpecification(userId, request.CourseId),
            cancellationToken);

        if (alreadyEnrolled)
            return Result.Fail(new ConflictError("You are already enrolled in this course."));

        await wishlistRepository.AddIfMissingAsync(userId, request.CourseId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
