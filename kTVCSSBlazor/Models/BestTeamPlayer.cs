namespace kTVCSSBlazor.Models
{
    public class BestTeamPlayer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? PhotoUrl { get; set; }
        public int MMR { get; set; }
        public double AVG { get; set; }
        public double KDR { get; set; }
        public double HSR { get; set; }
        public double Winrate { get; set; }
    }
}
