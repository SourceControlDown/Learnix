using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.CreateVideoLesson;

internal sealed class CreateVideoLessonCommandHandler(
    ICourseRepository courseRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<CreateVideoLessonCommand, Result<Guid>>(
        courseRepository, currentUser, lessonsBySectionId: true)
{
    protected override async Task<Result<Guid>> HandleAsync(
        CreateVideoLessonCommand request, Course course, CancellationToken cancellationToken)
    {
        var commitResult = await blobStorage.CommitUploadAsync(
            request.VideoBlobPath, UploadTarget.LessonVideo, cancellationToken);

        if (commitResult.IsFailed)
            return Result.Fail(commitResult.Errors);

        var lesson = VideoLesson.Create(
            request.SectionId,
            request.Title,
            commitResult.Value.BlobPath,
            request.Description,
            request.DurationSeconds);

        course.AddLesson(lesson);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(lesson.Id);
    }
}
