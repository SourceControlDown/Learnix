using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Commands.DeleteCourse;

public sealed class DeleteCourseCommandHandler
    : CourseCommandHandler<DeleteCourseCommand, Result>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCourseCommandHandler(
        ICurrentUserService currentUser,
        ICourseRepository courseRepository,
        IUnitOfWork unitOfWork)
        : base(courseRepository, currentUser)
    {
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<Result> HandleAsync(
        DeleteCourseCommand request, Course course, CancellationToken ct)
    {
        course.MarkForDeletion();

        await _courseRepository.DeleteAsync(course, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
