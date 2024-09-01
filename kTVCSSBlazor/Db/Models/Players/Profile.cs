namespace kTVCSSBlazor.Db.Models.Players
{
    public class Profile
    {
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? AvatarUrl { get; set; }
        public string? HeaderUrl { get; set; }
        public string? Description { get; set; }
        public bool NeedTeam { get; set; }
        public string? PreferredRole { get; set; }
        public string? LastTeam { get; set; }
        public string? PrimeTime { get; set; }
        public string? StartPlayYear { get; set; }
        public bool Microphone { get; set; }
        public bool TeamSpeak { get; set; }
        public bool Discord { get; set; }
        public string? Telegram { get; set; }
        public string VkId { get; set; }
        public string TwitchUrl { get; set; }
        public string YoutubeUrl { get; set; }
    }
}
