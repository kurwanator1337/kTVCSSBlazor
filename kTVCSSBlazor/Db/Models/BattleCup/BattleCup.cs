namespace kTVCSSBlazor.Db.Models.BattleCup
{
    public class BattleCup : Cup
    {
        public List<Match> Matches { get; set; }
        public List<Team> Members { get; set; }
    }
}
