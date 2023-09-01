using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

using Blog.Posts.API.Models;

namespace Blog.Posts.API.Data
{
    public class ApplicationContext
    {
        private static bool _configured = false;

        public IMongoCollection<Post> Posts { get; set; }

        public ApplicationContext(IMongoDatabase database)
        {
            Initialize();

            Posts = database.GetCollection<Post>("posts");
        }

        private static void Initialize()
        {
            if (_configured)
                return;

            var objectSerializer = new ObjectSerializer(type => true);

            BsonSerializer.RegisterSerializer(objectSerializer);

            _configured = true;
        }
    }
}
