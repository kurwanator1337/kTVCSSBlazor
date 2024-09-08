using kTVCSSBlazor.Components.Pages;
using kTVCSSBlazor.Components.Pages.Players.ProfileItems;
using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Repository;
using GameServers = kTVCSSBlazor.Db.Repository.GameServers;

namespace kTVCSSBlazor.Db;

public class kTVCSSRepository : Context, IRepository
{
    private IConfiguration configuration { get; set; }
    private ILogger logger { get; set; }

    private IVips _vips;
    private IAdmins _admins;
    private IFAQ _faq;
    private IGameServers _gameServers;
    private IHighlights _highlights;
    private IIM _im;
    private IMatches _matches;
    private IModerators _moderators;
    private IPlayers _players;
    private ITeams _teams;
    private IUserFeed _userFeed;

    public kTVCSSRepository(IConfiguration configuration, ILogger logger) : base(configuration, logger)
    {
        this.logger = logger;
        this.configuration = configuration;
    }

    public IAdmins Admins
    {
        get
        {
            if (_admins == null)
            {
                _admins = new Admins(configuration, logger);
            }
            return _admins;
        }
    }

    public IFAQ FAQ
    {
        get
        {
            if (_faq == null)
            {
                _faq = new FAQ(configuration, logger);
            }
            return _faq;
        }
    }

    public IGameServers GameServers
    {
        get
        {
            if (_gameServers == null)
            {
                _gameServers = new GameServers(configuration, logger);
            }
            return _gameServers;
        }
    }

    public IHighlights Highlights
    {
        get
        {
            if (_highlights == null)
            {
                _highlights = new Highlights(configuration, logger, Vips);
            }
            return _highlights;
        }
    }

    public IIM IM
    {
        get
        {
            if (_im == null)
            {
                _im = new IM(configuration, logger);
            }
            return _im;
        }
    }

    public IMatches Matches
    {
        get
        {
            if (_matches == null)
            {
                _matches = new Matches(configuration, logger);
            }
            return _matches;
        }
    }

    public IModerators Moderators
    {
        get
        {
            if (_moderators == null)
            {
                _moderators = new Moderators(configuration, logger);
            }
            return _moderators;
        }
    }

    public IPlayers Players
    {
        get
        {
            if (_players == null)
            {
                _players = new Players(configuration, logger);
            }
            return _players;
        }
    }

    public ITeams Teams
    {
        get
        {
            if (_teams == null)
            {
                _teams = new Teams(configuration, logger);
            }
            return _teams;
        }
    }

    public IUserFeed UserFeed
    {
        get
        {
            if (_userFeed == null)
            {
                _userFeed = new UserFeed(configuration, logger);
            }
            return _userFeed;
        }
    }

    public IVips Vips
    {
        get
        {
            if (_vips == null)
            {
                _vips = new Vips(configuration, logger);
            }
            return _vips;
        }
    }
}