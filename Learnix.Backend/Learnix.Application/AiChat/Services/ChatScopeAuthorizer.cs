using FluentResults;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;

namespace Learnix.Application.AiChat.Services;

/// <summary>
/// Guards access to a chat scope. Shared by the session query, the clear command and the SSE stream —
/// the stream sits outside the MediatR pipeline, so the rule has to live somewhere all three can reach.
/// </summary>
public sealed class ChatScopeAuthorizer(IEnrollmentRepository enrollmentRepository)
{
    public async Task<Result> EnsureAccessAsync(Guid userId, ChatScope scope, CancellationToken cancellationToken)
    {
        if (scope.Type != ChatScopeType.Course)
            return Result.Ok();

        var isEnrolled = await enrollmentRepository.AnyAsync(
            new ActiveEnrollmentByStudentAndCourseSpecification(userId, scope.CourseId!.Value), cancellationToken);

        return isEnrolled
            ? Result.Ok()
            : Result.Fail(new ForbiddenError(CommonMessages.NotEnrolledInCourse));
    }
}
