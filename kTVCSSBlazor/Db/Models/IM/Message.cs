namespace kTVCSSBlazor.Db.Models.IM
{
    public class Message
    {
        public string Text { get; set; }
        public DateTime DateTime { get; set; }
        public int FromID { get; set; }
        public string FromName { get; set; }
        public string FromAvatar { get; set; }
        public int ToID { get; set; }
        public string ToName { get; set; }
        public string ToAvatar { get; set; }
    }
}
