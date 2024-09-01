namespace kTVCSSBlazor.Db.Models.BattleCup
{
    public class Match
    {
        public int BID { get; set; }
        public int ATID { get; set; }
        public int BTID { get; set; }
        public int AScore { get; set; }
        public int BScore { get; set; }
        public int WID {  get; set; }
        public Part Part {  get; set; }
        public int SID { get; set; }
        public DateTime DtStart { get; set; }
        public DateTime DtEnd { get; set; }
        public int BracketPos { get; set; }
    }
}
