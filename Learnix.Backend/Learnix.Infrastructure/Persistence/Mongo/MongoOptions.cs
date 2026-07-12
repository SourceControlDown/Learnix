namespace Learnix.Infrastructure.Persistence.Mongo;

public sealed class MongoOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "learnix";
}
