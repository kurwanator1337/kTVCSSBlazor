namespace kTVCSSBlazor.Db.Models.Highlights
{
    public class Result
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public double Ticks { get; set; }
        public string DemoName { get; set; }
        public double Length { get; set; }
    }

    public class Match
    {
        public int Id { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public string Demo { get; set; }
    }
}
