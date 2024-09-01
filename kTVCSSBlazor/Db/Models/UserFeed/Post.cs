namespace kTVCSSBlazor.Db.Models.UserFeed
{
    public class Post
    {
        public string AuthorName { get; set; }
        public int AuthorId { get; set; }
        public string AuthorAvatar { get; set; }
        public int PostId { get; set; }
        public DateTime PostDate { get; set; }
        public string Content { get; set; }
        public string MediaUrl { get; set; }
        public MediaType MediaType { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public string NewComment { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsVip { get; set; }
        public List<Like> Likes { get; set; } = new List<Like>();
        public int LikesCount => Likes.Count(l => l.IsLike);
        public int DislikesCount => Likes.Count(l => !l.IsLike);
    }
}
