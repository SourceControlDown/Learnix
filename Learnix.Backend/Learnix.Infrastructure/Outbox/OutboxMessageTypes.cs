namespace Learnix.Infrastructure.Outbox;

public static class OutboxMessageTypes
{
    public const string DeleteBlob = "DeleteBlob";
    public const string MarkBlobConfirmed = "MarkBlobConfirmed";
    public const string InstructorApprovedEmail = "InstructorApprovedEmail";
    public const string InstructorRejectedEmail = "InstructorRejectedEmail";
    public const string UserBannedEmail = "UserBannedEmail";
    public const string UserUnbannedEmail = "UserUnbannedEmail";
    public const string UserRoleChangedEmail = "UserRoleChangedEmail";
    public const string CourseAdminUnpublishedEmail = "CourseAdminUnpublishedEmail";
    public const string CourseAdminDeletedEmail = "CourseAdminDeletedEmail";
}

public record DeleteBlobPayload(string BlobPath);

public record MarkBlobConfirmedPayload(string BlobPath);
