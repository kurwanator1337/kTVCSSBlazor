using kTVCSSBlazor.Hubs;

namespace kTVCSSBlazor.Mixes
{
    public class FreeMix
    {
        private kTVCSSHub Hub { get; set; }

        public FreeMix(kTVCSSHub hub)
        {
            Hub = hub;
            Task.Run(async () =>
            {
                await hub.PlayersDirector();
            });
        }
    }
}
