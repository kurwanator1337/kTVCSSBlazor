namespace kTVCSSBlazor.Db.Models.UserFeed
{
    public class Comment
    {
        public string AuthorName { get; set; }
        public string AuthorAvatar { get; set; }
        public int AuthorId { get; set; }
        public DateTime CommentDate { get; set; }
        public string Content { get; set; }
        public int PostId { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsVip { get; set; }
    }
}
