using MongoDB.Bson.Serialization.Attributes;

namespace Blog.Posts.API.Models
{
    public class Post
    {
        [BsonId]
        public Guid Id { get; set; }

        public string Title { get; set; } = default!;

        public string Content { get; set; } = default!;

        public Guid AuthorId { get; set; }

        public Post(string title, string content, Guid authorId)
        {
            Id = Guid.NewGuid();
            Title = title;
            Content = content;
            AuthorId = authorId;
        }
    }
}
