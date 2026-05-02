using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace Learnix.Infrastructure.Persistence.Mongo.Conventions;

public static class MongoConventionRegistration
{
    private static bool _registered;
    private static readonly Lock _lock = new();

    public static void Register()
    {
        lock (_lock)
        {
            if (_registered) return;

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            };

            ConventionRegistry.Register("LearnixMongoConventions", pack, _ => true);
            _registered = true;
        }
    }
}
