using kTVCSSBlazor.Db.Interfaces;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Data.Common;
using System.Net;
using Okolni.Source.Query;

namespace kTVCSSBlazor.Db.Repository
{
    public class GameServers(string connectionString) : IGameServers
    {
        private SqlConnection Db { get; set; } = new SqlConnection(connectionString);
        private string ConnectionString { get; set; } = connectionString;

        private void EnsureConnected()
        {
            try
            {
                if (Db.State != ConnectionState.Open)
                {
                    Db = new SqlConnection(Db.ConnectionString);
                    Db.Open();
                }
            }
            catch (Exception)
            {
                Db = new SqlConnection(ConnectionString);
                EnsureConnected();
            }
        }

        public List<dynamic> Get()
        {
            EnsureConnected();

            var data = Db.Query("SELECT * FROM GameServers WHERE ENABLED = 1 AND (TYPE = 0 OR TYPE = 1)");

            return data.ToList();
        }
    }
}
