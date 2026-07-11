using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Infrastructure.Outbox.Payloads;

namespace Learnix.Infrastructure.Outbox.Handlers.Emails;

/// <summary>Emails an admin's decision produces: on an instructor application, or on somebody's course.</summary>
internal sealed class InstructorApprovedEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendInstructorApprovedEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.InstructorApprovedEmail;

    protected override Task HandleAsync(SendInstructorApprovedEmailPayload payload, CancellationToken ct) =>
        emailSender.SendInstructorApplicationApprovedAsync(
            payload.ToEmail, payload.FirstName, payload.Language, ct);
}

internal sealed class InstructorRejectedEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendInstructorRejectedEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.InstructorRejectedEmail;

    protected override Task HandleAsync(SendInstructorRejectedEmailPayload payload, CancellationToken ct) =>
        emailSender.SendInstructorApplicationRejectedAsync(
            payload.ToEmail, payload.FirstName, payload.RejectionReason, payload.Language, ct);
}

internal sealed class CourseAdminUnpublishedEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendCourseAdminActionEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.CourseAdminUnpublishedEmail;

    protected override Task HandleAsync(SendCourseAdminActionEmailPayload payload, CancellationToken ct) =>
        emailSender.SendCourseAdminUnpublishedAsync(
            payload.ToEmail, payload.InstructorFirstName, payload.CourseTitle, payload.Language, ct);
}

internal sealed class CourseAdminDeletedEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendCourseAdminActionEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.CourseAdminDeletedEmail;

    protected override Task HandleAsync(SendCourseAdminActionEmailPayload payload, CancellationToken ct) =>
        emailSender.SendCourseAdminDeletedAsync(
            payload.ToEmail, payload.InstructorFirstName, payload.CourseTitle, payload.Language, ct);
}
