using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Specifications;
using Learnix.Domain.Constants;
using MediatR;

using Learnix.Application.Common.Pagination;

namespace Learnix.Application.Messaging.Queries.GetMyConversations;

internal sealed class GetMyConversationsQueryHandler(
    ICurrentUserService currentUser,
    IConversationRepository conversationRepository)
    : IRequestHandler<GetMyConversationsQuery, Result<PaginatedResult<ConversationSummaryDto>>>
{
    public async Task<Result<PaginatedResult<ConversationSummaryDto>>> Handle(
        GetMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;
        var isInstructor = currentUser.IsInRole(Roles.Instructor);
        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        Ardalis.Specification.ISpecification<Learnix.Domain.Entities.CourseConversation> spec = isInstructor
            ? new ConversationsByInstructorSpecification(userId, pagination.Skip, pagination.Take, request.SearchQuery)
            : new ConversationsByStudentSpecification(userId, pagination.Skip, pagination.Take, request.SearchQuery);

        var totalCount = await conversationRepository.CountAsync(spec, cancellationToken);
        if (totalCount == 0)
            return Result.Ok(PaginatedResult<ConversationSummaryDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var conversations = await conversationRepository.ListAsync(spec, cancellationToken);

        var dtos = conversations.Select(c =>
        {
            var otherUser = isInstructor ? c.Student! : c.Instructor!;
            var unreadCount = isInstructor ? c.InstructorUnreadCount : c.StudentUnreadCount;

            return new ConversationSummaryDto(
                c.Id,
                c.CourseId,
                c.Course!.Title,
                otherUser.Id,
                $"{otherUser.FirstName} {otherUser.LastName}",
                otherUser.AvatarBlobPath,
                c.LastMessagePreview,
                c.LastMessageAt,
                unreadCount);
        }).ToList();

        return Result.Ok(PaginatedResult<ConversationSummaryDto>.Create(dtos, pagination.PageIndex, pagination.PageSize, totalCount));
    }
}
