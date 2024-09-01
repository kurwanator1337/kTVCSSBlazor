using kTVCSSBlazor.Data;

namespace kTVCSSBlazor.Hubs.Models
{
    public class Mix
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string MapName { get; set; }
        public string MapImage { get; set; }
        public List<AwaitingPlayer> MixPlayers { get; set; } = new List<AwaitingPlayer>();
        public int ServerID { get; set; }
        public string ServerAddress { get; set; }
        public DateTime DtStart { get; set; } = DateTime.Now.AddMinutes(5);
    }
}
