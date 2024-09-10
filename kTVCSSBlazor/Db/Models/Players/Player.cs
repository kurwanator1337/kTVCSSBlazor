namespace kTVCSSBlazor.Db.Models.Players
{
    public class TotalPlayer
    {
        public int Position { get; set; }
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? SteamID { get; set; }
        public int? Kills { get; set; }
        public int? Deaths { get; set; }
        public int? Headshots { get; set; }
        public double? KDR { get; set; }
        public string? HSR { get; set; }
        public int? MMR { get; set; }
        public double? AVG { get; set; }
        public string? RankName { get; set; }
        public int? MatchesTotal { get; set; }
        public double Wons { get; set; }
        public double Losts { get; set; }
        public int? Calibration { get; set; }
        public string? LastMatch { get; set; }
        public string? Winrate { get; set; }
        public int? Block { get; set; }
        public string? PhotoUrl { get; set; } = "/images/logo_ktv.png";
        public string? HeaderPicture { get; set; }
        public int? Tier { get; set; }
        public bool IsOnline { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsVip { get; set; }
        public bool IsPremiumVip { get; set; }
    }

    public class BannedUser : TotalPlayer
    {
        public string? Reason { get; set; }
        public string? BannedBy { get; set; }
        public string? BanExpires { get; set; }
    }

    public class Player
    {
        public int? Position { get; set; }
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Steamid { get; set; }

        public double? Kills { get; set; }

        public double? Deaths { get; set; }

        public double? Headshots { get; set; }

        public double? Kdr { get; set; }

        public double? Hsr { get; set; }

        public int? Mmr { get; set; }

        public double? Avg { get; set; }

        public string? Rankname { get; set; }

        public double? Matchesplayed { get; set; }

        public double? Matcheswins { get; set; }

        public double? Matcheslooses { get; set; }

        public byte? Iscalibration { get; set; }

        public DateTime? Lastmatch { get; set; }

        public string? Vkid { get; set; }

        public byte? Anounce { get; set; }

        public double? Winrate { get; set; }

        public byte? Block { get; set; }

        public string? Blockreason { get; set; }

        public DateTime? Banexpires { get; set; }

        public string? Firstname { get; set; }

        public string? Lastname { get; set; }

        public string? Photo { get; set; }

        public string? Headerpicture { get; set; }

        public int? Banmultiplier { get; set; }

        public int? Anticheatrequired { get; set; }

        public int? Camrequired { get; set; }

        public string? Password { get; set; }

        public string? Confirmcode { get; set; }

        public string? Login { get; set; }
    }
}
