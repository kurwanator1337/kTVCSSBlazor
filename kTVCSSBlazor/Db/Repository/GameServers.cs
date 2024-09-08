using kTVCSSBlazor.Db.Interfaces;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Data.Common;
using System.Net;
using Okolni.Source.Query;

namespace kTVCSSBlazor.Db.Repository
{
    public class GameServers(IConfiguration configuration, ILogger logger) : Context(configuration, logger), IGameServers
    {
        public List<dynamic> Get()
        {
            EnsureConnected();

            var data = Db.Query("SELECT * FROM GameServers WHERE ENABLED = 1 AND (TYPE = 0 OR TYPE = 1)");

            return data.ToList();
        }
    }
}
