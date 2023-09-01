namespace Blog.Authors.gRPC.Models
{
    public class Author
    {
        public Guid Id { get; private set; }

        public string Name { get; set; } = default!;
    }
}
