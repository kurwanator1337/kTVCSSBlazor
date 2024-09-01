using Dapper;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;
using static kTVCSSBlazor.Db.Models.Matches.MatchInfo;

namespace kTVCSSBlazor.Hubs
{
    public class MatchLog
    {
        public DateTime DateTime { get; set; }
        public string Message { get; set; }
    }

    public class STVDirectorHub(string connectionString) : Hub
    {
        private string _connectionString { get; set; } = connectionString;
        private SqlConnection Db { get; set; } = new SqlConnection(connectionString);

        private void EnsureConnected()
        {
            try
            {
                if (Db.State != ConnectionState.Open)
                {
                    Db = new SqlConnection(_connectionString);
                    Db.Open();
                }
            }
            catch (Exception)
            {
                Db = new SqlConnection(_connectionString);
                EnsureConnected();
            }
        }

        public async Task Start(int serverID)
        {
            int matchID = 0;

            // тут поток блокируется, поэтому другой не может подключаться
            while (matchID == 0)
            {
                EnsureConnected();
                matchID = Db.QueryFirstOrDefault<int>($"SELECT ID FROM [kTVCSS].[dbo].[MatchesLive] WHERE FINISHED = 0 AND SERVERID = {serverID}");
                await Task.Delay(3000);
                try
                {
                    Clients.Caller.SendAsync("GetMessage", "Ожидаем начало матча...");
                }
                catch (Exception)
                {
                    // failed
                }
            }

            try
            {
                Clients.Caller.SendAsync("GetMessage", $"Начинаем отслеживать матч {matchID}!");
                Clients.Caller.SendAsync("GetMatchID", matchID);
            }
            catch (Exception)
            {

            }
        }

        public async Task SendMatchLogs(int matchID)
        {
            EnsureConnected();

            try
            {
                var log = Db.Query<MatchLog>($"SELECT [DATETIME], [MESSAGE] FROM [kTVCSS].[dbo].[ViewMatchesLogs] WITH (NOLOCK) WHERE (DATETIME BETWEEN DATEADD(MINUTE, -2, GETDATE()) AND GETDATE()) AND MESSAGE LIKE '%> killed%' AND MATCHID = {matchID} ORDER BY DATETIME ASC");

                Clients.Caller.SendAsync("GetMatchLogs", log.Distinct().ToList());
            }
            catch (Exception)
            {
                // failed
            }
        }
    }
}
