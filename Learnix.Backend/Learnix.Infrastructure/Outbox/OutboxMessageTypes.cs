namespace Learnix.Infrastructure.Outbox;

public static class OutboxMessageTypes
{
    public const string DeleteBlob = "DeleteBlob";
    public const string MarkBlobConfirmed = "MarkBlobConfirmed";
    public const string InstructorApprovedEmail = "InstructorApprovedEmail";
    public const string InstructorRejectedEmail = "InstructorRejectedEmail";
}

public record DeleteBlobPayload(string BlobPath);

public record MarkBlobConfirmedPayload(string BlobPath);
