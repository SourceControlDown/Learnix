using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Sections.Commands.ReorderSections;

internal sealed class ReorderSectionsCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<ReorderSectionsCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        ReorderSectionsCommand request, Course course, CancellationToken ct)
    {
        var pairs = request.Items.Select(i => (i.Id, i.Order)).ToList();

        course.ReorderSections(pairs);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
