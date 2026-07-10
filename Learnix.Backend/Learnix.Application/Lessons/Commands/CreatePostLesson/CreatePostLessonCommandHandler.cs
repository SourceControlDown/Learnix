using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.CreatePostLesson;

internal sealed class CreatePostLessonCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<CreatePostLessonCommand, Result<Guid>>(
        courseRepository, currentUser, lessonsBySectionId: true)
{
    protected override async Task<Result<Guid>> HandleAsync(
        CreatePostLessonCommand request, Course course, CancellationToken ct)
    {
        var lesson = PostLesson.Create(request.SectionId, request.Title, request.Content);

        course.AddLesson(lesson);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(lesson.Id);
    }
}
