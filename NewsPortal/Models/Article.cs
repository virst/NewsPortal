namespace NewsPortal.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public List<Comment> Comments { get; set; } = [];
    }
}
