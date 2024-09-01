namespace kTVCSSBlazor.Db.Models.Teams
{
    public class Team
    {
        public int Position { get; set; }
        public int PlayersCount { get; set; }
        public string AvatarUrl { get; set; } = "/images/logo_ktv.png";
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CapSteamID { get; set; }
        public string? CapVKID { get; set; }
        public int MatchesPlayed { get; set; }
        public int MatchesWins { get; set; }
        public int MatchesLosts { get; set; }
        public int Rating { get; set; }
        public string? WinRate { get; set; }
        public int BlockEdit { get; set; }
        public int? Tier { get; set; }
        public string? Description { get; set; }
    }
}
