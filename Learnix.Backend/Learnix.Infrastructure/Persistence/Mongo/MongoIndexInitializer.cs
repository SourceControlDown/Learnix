using Learnix.Infrastructure.Persistence.Mongo.Documents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Learnix.Infrastructure.Persistence.Mongo;

internal sealed class MongoIndexInitializer(
    MongoDbContext context,
    ILogger<MongoIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            // (userId, scope, courseId) identifies a session. Unique, so a concurrent first message fails
            // with a duplicate key instead of quietly forking the history into two documents.
            var indexKeys = Builders<ChatSessionDocument>.IndexKeys
                .Ascending(s => s.UserId)
                .Ascending(s => s.Scope)
                .Ascending(s => s.CourseId);

            var indexModel = new CreateIndexModel<ChatSessionDocument>(
                indexKeys,
                new CreateIndexOptions { Name = "UX_chat_sessions_userId_scope_courseId", Unique = true });

            await context.ChatSessions.Indexes.CreateOneAsync(indexModel, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // Most likely cause: pre-scope documents already in the collection, several per user, so the
            // unique index cannot be built. Startup continues, but sessions can fork until it is resolved.
            logger.LogError(
                ex,
                "Failed to create the unique chat session index. Session uniqueness is NOT enforced. "
                + "If the collection holds legacy documents without a scope, drop it: db.chat_sessions.drop()");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
