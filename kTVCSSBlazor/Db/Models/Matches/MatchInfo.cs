using kTVCSSBlazor.Models;

namespace kTVCSSBlazor.Db.Models.Matches
{
    public class MatchInfo
    {
        public double AScore { get; set; }
        public double BScore { get; set; }
        public string? AName { get; set; }
        public string? AID { get; set; }
        public string AAvatarUrl { get; set; } = "/images/logo_ktv.png";
        public string? BName { get; set; }
        public string? BID { get; set; }
        public string BAvatarUrl { get; set; } = "/images/logo_ktv.png";
        public string? ServerName { get; set; }
        public string? MapName { get; set; }
        public string? DemoUrl { get; set; }
        public string? MatchLength { get; set; }
        public string? MatchDate { get; set; }
        public double ATeamAVG { get; set; } = 0;
        public string? MVP { get; set; }
        public double BTeamAVG { get; set; } = 0;
        public List<double> ATeamMMR { get; set; } = [];
        public List<double> BTeamMMR { get; set; } = [];
        public List<MPlayerInfo> AMPlayers { get; set; } = [];
        public List<PopoverItem> APopovers { get; set; } = [];
        public List<PopoverItem> BPopovers { get; set; } = [];
        public List<MPlayerInfo> BMPlayers { get; set; } = [];
        public List<MatchStatsGrid> ATeamGrid { get; set; } = [];
        public List<MatchStatsGrid> BTeamGrid { get; set; } = [];
        public List<Log> MatchLog { get; set; } = [];
        public bool IsFinished { get; set; }
        public BestTeamPlayer? ABestPlayer { get; set; }
        public BestTeamPlayer? BBestPlayer { get; set; }

        public class MPlayerInfo
        {
            private string? name;
            public string? ID { get; set; }
            public string? Name { get; set; }
            public string DisplayName
            {
                get
                {
                    return name;
                }
                set
                {
                    string t = value;
                    name = t.Length > 12 ? name = t.Substring(0, 12) : name = t;
                }
            }
            public string? Url { get; set; }
            public string AvatarUrl { get; set; } = "/images/logo_ktv.png";
            public string? SteamID { get; set; }
            public string? VkID { get; set; }
        }

        public class MatchStatsGrid
        {
            public string? ID { get; set; }
            public string? Name { get; set; }
            public string? SteamID { get; set; }
            public string? PictureUrl { get; set; }
            public string AvatarUrl { get; set; } = "/images/logo_ktv.png";
            public string? VkID { get; set; }
            public double Kills { get; set; }
            public double Deaths { get; set; }
            public double KRR { get; set; }
            public double KDR { get; set; }
            public double Headshots { get; set; }
            public double HSR { get; set; }
            public double Damage { get; set; }
            public double ADR { get; set; }
            public int MMR { get; set; }
            public int OpenFrags { get; set; }
            public int Triples { get; set; }
            public int Quadros { get; set; }
            public int Aces { get; set; }
        }

        public class Log
        {
            public string? Record { get; set; }
        }

        public class PopoverItem
        {
            public string? Name { get; set; }
            public string? MMR { get; set; }
            public string? KDR { get; set; }
            public string? AVG { get; set; }
        }
    }
}
