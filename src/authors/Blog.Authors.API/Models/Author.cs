namespace Blog.Authors.API.Models
{
    public class Author
    {
        private readonly Guid _id;

        public Guid Id => _id;

        public string Name { get; set; } = default!;

        public Author(string name)
        {
            _id = Guid.NewGuid();

            Name = name;
        }
    }
}
