using kTVCSSBlazor.Db.Interfaces;

namespace kTVCSSBlazor.Db
{
    public interface IRepository
    {
        public IAdmins Admins { get; }
        public IFAQ FAQ { get; }
        public IGameServers GameServers { get; }
        public IHighlights Highlights { get; }
        public IIM IM { get; }
        public IMatches Matches { get; }
        public IModerators Moderators { get; }
        public IPlayers Players { get; }
        public ITeams Teams { get; }
        public IUserFeed UserFeed { get; }
        public IVips Vips { get; }
    }
}
