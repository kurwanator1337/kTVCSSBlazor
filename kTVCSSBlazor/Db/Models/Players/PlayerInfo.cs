using kTVCSSBlazor.Db.Models.Teams;

namespace kTVCSSBlazor.Db.Models.Players
{
    public class PlayerInfo
    {
        public string? Name { get; set; }
        public string? SureName { get; set; }
        public string? RankUrl { get; set; }
        public string? AvatarUrl { get; set; } = "/images/logo_ktv.png";
        public string? TopMap { get; set; }
        public string? TopWeapon { get; set; }
        public string? LastFiveMatches { get; set; }
        public string? Role { get; set; }
        public bool Vip { get; set; } = false;
        public string? IsPlayingNow { get; set; }
        public string? HeaderPicture { get; set; }
        public string? Description { get; set; }
        public List<Rank> Ranks { get; set; } = [];
        public List<Friend> Friends { get; set; } = [];
        public List<Friend> TeamMates { get; set; } = [];
        public MainInfo MainInfo { get; set; } = new MainInfo();
        public Highlights Highlights { get; set; } = new Highlights();
        public List<LastTwentyMatches> LastTwentyMatches { get; set; } = [];
        public List<LastTwentyMatches> BestMatches { get; set; } = [];
        public List<Rating> Rating { get; set; } = [];
        public List<Weapons> Weapons { get; set; } = [];
        public List<ChatHistory> Chat { get; set; } = [];
        public AvgInfo AvgInfo { get; set; } = new AvgInfo();
        public TeamInfo TeamInfo { get; set; } = new TeamInfo();
        public List<Nick> NicknameHistory { get; set; } = [];
        public List<PlayerReport> Reports { get; set; } = [];
        public string? BanExpires { get; set; }
        public string? VKDonut { get; set; }
        public double ProjectRating { get; set; }
        public string Achiviments { get; set; } = "";
        public List<Achiviment> NormalAchiviments { get; set; } = new List<Achiviment>();
        public Social Social { get; set; } = new Social();
        public Behavior BanMultiplier { get; set; } = 0;
    }
    
    public enum Behavior
    {
        Безупречная = 0,
        Отличная = 1,
        Хорошая = 2,
        Средняя = 3,
        Умеренная = 4,
        Плохая = 5,
        Ужасная = 6,
        Отвратительная = 7
    }

    public class Social
    {
        public long TelegramId { get; set; }
        public string VkId { get; set; }
        public string TwitchUrl { get; set; }
        public string YoutubeUrl { get; set; }
    }

    public class Friend
    {
        public string? Name { get; set; }
        public int Id { get; set; }
        public string? Photo { get; set; }
    }

    public class PlayerReport
    {
        public string? Text { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class Nick
    {
        public string? Name { get; set; }
    }

    public class TeamInfo
    {
        public string? Name { get; set; }
        public string? ID { get; set; }
    }

    public class ChatHistory
    {
        public string? Message { get; set; }
        public string? Server { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class AvgInfo
    {
        public double AvgKills { get; set; }
        public double AvgDeaths { get; set; }
        public double AvgHeadshots { get; set; }
        public double AvgKdr { get; set; }
        public string? AvgHsr { get; set; }
    }

    public class Highlights
    {
        public int OpenFrags { get; set; }
        public int Tripples { get; set; }
        public int Quadros { get; set; }
        public int Aces { get; set; }
    }

    public class Weapons
    {
        public string? Weapon { get; set; }
        public int Count { get; set; }
    }

    public class Rating
    {
        public double Points { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class LastTwentyMatches
    {
        public int ID { get; set; }
        public string? Result { get; set; }
        public DateTime DateTime { get; set; }
        public string? MapName { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Headshots { get; set; }
        public bool Victory { get; set; }
        public string? Link { get; set; }
    }

    public class MainInfo : TotalPlayer
    {
        public string GameHours { get; set; } = "0";
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public string? BlockReason { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public string? VkId { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public string? PhotoUrl { get; set; }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    }
}
