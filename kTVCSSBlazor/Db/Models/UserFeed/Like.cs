namespace kTVCSSBlazor.Db.Models.UserFeed
{
    public class Like
    {
        public int LikeId { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public bool IsLike { get; set; }  // true для лайка, false для дизлайка

        public virtual Post Post { get; set; }
    }
}
