namespace Learnix.Infrastructure.Outbox;

public static class OutboxMessageTypes
{
    public const string DeleteBlob = "DeleteBlob";
    public const string MarkBlobConfirmed = "MarkBlobConfirmed";
}

public record DeleteBlobPayload(string BlobPath);

public record MarkBlobConfirmedPayload(string BlobPath);
