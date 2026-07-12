namespace Learnix.Application.AiChat.Abstractions.Models;

/// <summary>
/// Where the message was sent from. Supplied by the request, never by the model — a tool that took the
/// lesson id as a model argument could be talked into reading a lesson the user is not enrolled in.
/// The user comes from the JWT, so it is not repeated here.
/// </summary>
public sealed record ChatToolContext(Guid? CourseId, Guid? LessonId);

/// <param name="ArgumentsJson">Arguments the model produced, matching the tool's JSON schema.</param>
public sealed record ChatToolInvocation(string ArgumentsJson, ChatToolContext Context);
