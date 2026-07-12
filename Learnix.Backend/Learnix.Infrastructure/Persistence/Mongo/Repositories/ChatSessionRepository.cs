using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Learnix.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class ChatSessionRepository(MongoDbContext context) : IChatSessionRepository
{
    public async Task<ChatSession?> GetByScopeAsync(Guid userId, ChatScope scope, CancellationToken cancellationToken = default)
    {
        var doc = await context.ChatSessions
            .Find(ScopeFilter(userId, scope))
            .FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : MapToModel(doc);
    }

    public async Task<ChatSession> GetOrCreateAsync(Guid userId, ChatScope scope, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // UserId, Scope and CourseId come from the filter's equality terms, so Mongo writes them on insert.
        // Restating them in $setOnInsert would conflict.
        var update = Builders<ChatSessionDocument>.Update
            .SetOnInsert(s => s.CreatedAt, now)
            .SetOnInsert(s => s.UpdatedAt, now)
            .SetOnInsert(s => s.Messages, []);

        var options = new FindOneAndUpdateOptions<ChatSessionDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var doc = await context.ChatSessions.FindOneAndUpdateAsync(
            ScopeFilter(userId, scope), update, options, cancellationToken);

        return MapToModel(doc);
    }

    public async Task AppendMessagesAsync(
        string sessionId,
        IEnumerable<ChatMessage> messages,
        int storedMessagesLimit,
        CancellationToken cancellationToken = default)
    {
        var docs = messages.Select(MapMessageToDocument).ToList();

        // $push + $each + $slice trims to the newest N in the same atomic write, so two tabs appending
        // at once cannot lose each other's messages the way a read-trim-write would.
        var update = Builders<ChatSessionDocument>.Update
            .PushEach(s => s.Messages, docs, slice: -storedMessagesLimit)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var filter = Builders<ChatSessionDocument>.Filter.Eq(s => s.Id, MongoDB.Bson.ObjectId.Parse(sessionId));
        await context.ChatSessions.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, ChatScope scope, CancellationToken cancellationToken = default)
    {
        await context.ChatSessions.DeleteOneAsync(ScopeFilter(userId, scope), cancellationToken);
    }

    public async Task<long> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ChatSessionDocument>.Filter.Eq(s => s.UserId, userId);
        var result = await context.ChatSessions.DeleteManyAsync(filter, cancellationToken);

        return result.DeletedCount;
    }

    private static FilterDefinition<ChatSessionDocument> ScopeFilter(Guid userId, ChatScope scope) =>
        Builders<ChatSessionDocument>.Filter.And(
            Builders<ChatSessionDocument>.Filter.Eq(s => s.UserId, userId),
            Builders<ChatSessionDocument>.Filter.Eq(s => s.Scope, scope.Type),
            Builders<ChatSessionDocument>.Filter.Eq(s => s.CourseId, scope.CourseId));

    private static ChatSession MapToModel(ChatSessionDocument doc) => new()
    {
        Id = doc.Id.ToString(),
        UserId = doc.UserId,
        Messages = doc.Messages.Select(MapMessageToModel).ToList(),
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt
    };

    private static ChatMessage MapMessageToModel(ChatMessageDocument doc) => new(
        doc.Role,
        doc.Content,
        doc.SentAt,
        doc.ToolCalls?.Select(t => new ToolCall(t.CallId, t.ToolName, t.ArgumentsJson, t.ResultJson)).ToList(),
        doc.LessonId);

    private static ChatMessageDocument MapMessageToDocument(ChatMessage msg) => new()
    {
        Role = msg.Role,
        Content = msg.Content,
        SentAt = msg.SentAt,
        LessonId = msg.LessonId,
        ToolCalls = msg.ToolCalls?.Select(t => new ToolCallDocument
        {
            CallId = t.CallId,
            ToolName = t.ToolName,
            ArgumentsJson = t.ArgumentsJson,
            ResultJson = t.ResultJson
        }).ToList()
    };
}
