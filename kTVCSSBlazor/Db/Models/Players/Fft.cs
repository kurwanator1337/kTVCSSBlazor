namespace kTVCSSBlazor.Db.Models.Players
{
    public class Fft
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? RankName { get; set; }
        public double KDR { get; set; }
        public double HSR { get; set; }
        public double AVG { get; set; }
        public int MatchesTotal { get; set; }
        public string? Winrate { get; set; }
        public string? PreferredRole { get; set; }
        public string? LastTeam { get; set; }
        public string? PrimeTime { get; set; }
        public string? StartPlayYear { get; set; }
        public bool Microphone { get; set; }
        public bool TeamSpeak { get; set; }
        public bool Discord { get; set; }
    }
}
