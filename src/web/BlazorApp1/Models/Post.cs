namespace BlazorApp1.Models
{
    public class Post
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = default!;

        public string Content { get; set; } = default!;

        public Guid AuthorId { get; set; }
    }
}
