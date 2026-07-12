using Learnix.Application.AiChat.Abstractions.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Learnix.Infrastructure.Persistence.Mongo.Documents;

public sealed class ChatSessionDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Stored as a string by <c>EnumRepresentationConvention</c> — readable in the Mongo shell.</summary>
    public ChatScopeType Scope { get; set; }

    /// <summary>Null for the platform scope. Part of the unique key together with UserId and Scope.</summary>
    public Guid? CourseId { get; set; }

    public List<ChatMessageDocument> Messages { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
