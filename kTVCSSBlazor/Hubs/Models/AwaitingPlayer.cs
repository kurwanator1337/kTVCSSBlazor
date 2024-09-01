using kTVCSSBlazor.Data;

namespace kTVCSSBlazor.Hubs.Models
{
    public class AwaitingPlayer : User
    {
        public string ConnectionID { get; set; }
        public bool Ready { get; set; } = false;
        public string ProfileLink { get; set; }
    }
}
