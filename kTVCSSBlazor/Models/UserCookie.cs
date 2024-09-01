namespace kTVCSSBlazor.Models
{
    public class UserCookie
    {
        public string? VkUid { get; set; }
        public string? SteamId { get; set; }
        public int Id { get; set; }
        public string? Name { get; set; }
        public string TeamName { get; set; } = "Команда";
        public string TeamID { get; set; }
        public string? RankPicture { get; set; }
        public string? AvatarUrl { get; set; }
        public string TeamPicture { get; set; } = "card";
        public int CurrentMMR { get; set; }
        public int MaxMMR { get; set; }
        public int MinMMR { get; set; }
        public int TotalMatches { get; set; }
        public int Tier { get; set; } = 2;
    }
}
