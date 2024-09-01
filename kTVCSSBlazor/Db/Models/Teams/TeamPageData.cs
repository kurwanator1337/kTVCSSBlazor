using kTVCSSBlazor.Db.Models.Players;

namespace kTVCSSBlazor.Db.Models.Teams
{
    public class TeamPageData
    {
        public Team Info { get; set; } = new Team();
        public List<Player> Members { get; set; } = [];
        public List<TeamMatch> Matches { get; set; } = [];
        public List<Achiviment> Achiviments { get; set; } = [];
        public string? CreationDate { get; set; }
        public string AvatarUrl { get; set; } = "/images/logo_ktv.png";
    }

    public class Achiviment
    {
        public int Place { get; set; }
        public string? Name { get; set; }
    }

    public class Player : TotalPlayer
    {
        public string? EntryDate { get; set; }
    }

    public class TeamMatch
    {
        public int ID { get; set; }
        public string? Result { get; set; }
        public DateTime DateTime { get; set; }
        public string? MapName { get; set; }
        public bool Victory { get; set; }
    }
}
