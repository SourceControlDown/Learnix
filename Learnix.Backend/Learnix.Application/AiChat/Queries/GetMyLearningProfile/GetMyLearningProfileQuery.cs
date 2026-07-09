using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetMyLearningProfile;

/// <remarks>
/// Carries no user id on purpose — the subject is always the authenticated caller, read from
/// <c>ICurrentUserService</c>. A user id parameter would let a prompt-injected message read another
/// student's profile.
/// </remarks>
public sealed record GetMyLearningProfileQuery(IReadOnlyList<string>? Sections = null)
    : IRequest<Result<MyLearningProfileDto>>;
