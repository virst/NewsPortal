namespace NewsPortal.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = default!;
        public int Rating { get; set; }
        public int ArticleId { get; set; }
        public Article? Article { get; set; }
    }
}
