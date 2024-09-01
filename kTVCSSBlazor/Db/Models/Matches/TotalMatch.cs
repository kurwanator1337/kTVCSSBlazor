namespace kTVCSSBlazor.Db.Models.Matches
{
    public class TotalMatch
    {
        public int Id { get; set; }
        public string? MatchDate { get; set; }
        public string? ATeam { get; set; }
        public string? BTeam { get; set; }
        public string? Score { get; set; }
        public string? MapName { get; set; }
        public int ServerId { get; set; }
        public string? Link { get; set; }
    }

    public class TotalMatchEx : TotalMatch
    {
        public string? Name { get; set; }
        public string? Team { get; set; }
        public string? SteamID { get; set; }
        public string? Rank { get; set; }
        public string? VkID { get; set; }
        public double Kills { get; set; }
        public double Deaths { get; set; }
        public double Headshots { get; set; }
        public int OpenFrags { get; set; }
        public int Tripples { get; set; }
        public int Quadros { get; set; }
        public int Rampages { get; set; }
    }

    public class TotalMatchEx2 : TotalMatch
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? Team { get; set; }
        public string? SteamID { get; set; }
        public string? Rank { get; set; }
        public string? VkID { get; set; }
        public double Kills { get; set; }
        public double Deaths { get; set; }
        public double Headshots { get; set; }
        public int OpenFrags { get; set; }
        public int Tripples { get; set; }
        public int Quadros { get; set; }
        public int Rampages { get; set; }
        public double MMR { get; set; }
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public string? PhotoUrl { get; set; } = "/images/logo_ktv.png";
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    }

    public class Player
    {
        public string? Name { get; set; }
        public string? Team { get; set; }
    }
}
