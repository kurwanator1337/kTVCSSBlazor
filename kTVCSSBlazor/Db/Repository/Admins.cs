using Dapper;
using kTVCSSBlazor.Components.Pages;
using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Hubs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using VkNet.Model;

namespace kTVCSSBlazor.Db.Repository
{
    public class Admins(IConfiguration configuration, ILogger logger) : Context(configuration, logger), IAdmins
    {
        private void ExecuteCommand(string command)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = "/bin/bash";
                proc.StartInfo.Arguments = "-c \" " + command + " \"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    Console.WriteLine(proc.StandardOutput.ReadLine());
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task BanPlayer(int id, string reason, string admin)
        {
            EnsureConnected();

            try
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("ID", id);
                dynamicParameters.Add("REASON", reason);
                dynamicParameters.Add("BY", admin);
                dynamicParameters.Add("EXPIRES", DateTime.Now.AddDays(Models.Players.Block.Reasons[reason]));

                await Db.ExecuteAsync("BanPlayerByID", dynamicParameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                Logger.LogInformation($"{admin} забанил игрока https://ktvcss.ru/player/{id} с причиной {reason}");
            }
        }

        public async Task BlockTeam(int id, string admin)
        {
            EnsureConnected();

            Logger.LogInformation($"Заблокирован состав команды https://ktvcss.ru/team/{id} админом {admin}");

            await Db.ExecuteAsync($"UPDATE Teams SET ISBLOCKEDITING = 1 WHERE ID = {id}");
        }

        public async Task<bool> IsAdmin(string steam)
        {
            EnsureConnected();

            string vip = await Db.QueryFirstOrDefaultAsync<string>($"SELECT NAME FROM Admins WITH (NOLOCK) WHERE NAME = '{steam}'");

            if (!string.IsNullOrEmpty(vip))
            {
                return true;
            }
            else return false;
        }

        public async Task RestartAllNodes(string admin)
        {
            EnsureConnected();

            var servers = await Db.QueryAsync<string>($"SELECT ID FROM GameServers WHERE ENABLED = 1");

            foreach (var id in servers)
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("SERVERID", id);

                await Db.ExecuteAsync("StopMatch", dynamicParameters, commandType: CommandType.StoredProcedure);

                ExecuteCommand("systemctl restart node-ktvcss" + id);
            }

            Logger.LogInformation($"Перезапуск всех нод админом {admin}");

            kTVCSSHub.Mixes.Clear();
        }

        public async Task SetServerMatch(int id, string admin)
        {
            EnsureConnected();

            id++;

            await Db.ExecuteAsync($"UPDATE GameServers SET TYPE = 0 WHERE ID = {id}");

            ExecuteCommand("systemctl restart node-ktvcss" + id);

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("SERVERID", id);

            await Db.ExecuteAsync("StopMatch", dynamicParameters, commandType: CommandType.StoredProcedure);

            var guid = await Db.QueryFirstOrDefaultAsync<string>($"SELECT GUID FROM Mixes WHERE SERVERID = {id}");

            if (guid is not null)
            {
                kTVCSSHub.Mixes.RemoveAll(x => x.Guid.ToString().ToLower() == guid.ToString().ToLower());
            }

            Logger.LogInformation($"Сервер #{id} установлен как матч-сервер админом {admin}");
        }

        public async Task SetServerMix(int id, string admin)
        {
            EnsureConnected();

            id++;

            await Db.ExecuteAsync($"UPDATE GameServers SET TYPE = 1 WHERE ID = {id}");

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("SERVERID", id);

            await Db.ExecuteAsync("StopMatch", dynamicParameters, commandType: CommandType.StoredProcedure);

            ExecuteCommand("systemctl restart node-ktvcss" + id);

            var guid = await Db.QueryFirstOrDefaultAsync<string>($"SELECT GUID FROM Mixes WHERE SERVERID = {id}");

            if (guid is not null)
            {
                kTVCSSHub.Mixes.RemoveAll(x => x.Guid.ToString().ToLower() == guid.ToString().ToLower());
            }

            Logger.LogInformation($"Сервер #{id} установлен как микс-сервер админом {admin}");
        }

        public async Task StopMatch(int id, string admin)
        {
            EnsureConnected();

            id++;

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("SERVERID", id);

            await Db.ExecuteAsync("StopMatch", dynamicParameters, commandType: CommandType.StoredProcedure);

            ExecuteCommand("systemctl restart node-ktvcss" + id);

            var guid = await Db.QueryFirstOrDefaultAsync<string>($"SELECT GUID FROM Mixes WHERE SERVERID = {id}");

            if (guid is not null)
            {
                kTVCSSHub.Mixes.RemoveAll(x => x.Guid.ToString().ToLower() == guid.ToString().ToLower());
            }

            Logger.LogInformation($"Матч #{id} был остановлен админом {admin}");
        }

        public async Task UnbanPlayer(int id, string admin, string reason)
        {
            EnsureConnected();

            await Db.ExecuteAsync($"UPDATE Players SET BLOCK = 0 WHERE ID = {id}");

            Logger.LogInformation($"Игрок https://ktvcss.ru/player/{id} был разбанен админом {admin} с причиной {reason}");
        }

        public async Task SetMMR(int id, string admin, int mmr)
        {
            EnsureConnected();

            await Db.ExecuteAsync($"UPDATE Players SET MMR = {mmr} WHERE ID = {id}");

            Logger.LogInformation($"{admin} задал {mmr} ммр игроку https://ktvcss.ru/player/{id}");
        }

        public async Task UnbanRequest(int id, string reason)
        {
            Logger.LogInformation($"Игрок https://ktvcss.ru/player/{id} подал заявку на разбан с причиной {reason}");
        }

        public async Task UnBlockTeam(int id, string admin)
        {
            EnsureConnected();

            await Db.ExecuteAsync($"UPDATE Teams SET ISBLOCKEDITING = 0 WHERE ID = {id}");

            Logger.LogInformation($"Состав команды https://ktvcss.ru/team/{id} был разблокирован админом {admin}");
        }

        public async Task SetModerator(int id, string admin)
        {
            EnsureConnected();

            await Db.ExecuteAsync($"UPDATE Players SET MODERATOR = 1 WHERE ID = {id}");

            Logger.LogInformation($"Игрок https://ktvcss.ru/player/{id} был назначен модератором админом {admin}");
        }

        public async Task RemoveModerator(int id, string admin)
        {
            EnsureConnected();

            await Db.ExecuteAsync($"UPDATE Players SET MODERATOR = 0 WHERE ID = {id}");

            Logger.LogInformation($"Игрок https://ktvcss.ru/player/{id} был снят с должности модератора админом {admin}");
        }

        public async Task BugReport(int id, string text)
        {
            Logger.LogInformation($"https://ktvcss.ru/player/{id} сообщил о баге/ошибке {text}");
        }

        public async Task RestoreMMR()
        {
            EnsureConnected();

            var data = Db.Query("SELECT * FROM Players WHERE STEAMID IN (SELECT STEAMID FROM [kTVCSS].[dbo].[Players] WHERE STEAMID != 'STEAM_UNDEFINED' GROUP BY STEAMID HAVING COUNT(STEAMID) > 1) ORDER BY STEAMID ");

            var group = data.GroupBy(x => x.STEAMID);

            foreach (var item in group)
            {
                var order = item.OrderBy(x => x.ID);

                for (int i = 0; i < order.Count(); i++)
                {
                    if (i == order.Count()) continue;
                    else
                    {
                        Db.Execute($"DELETE FROM Players WHERE ID = {order.ElementAt(i).ID}");
                    }
                }
            }

            var ids = Db.Query<string>("SELECT STEAMID FROM Players WITH (NOLOCK)");

            foreach (var id in ids)
            {
                var mmr = Db.QuerySingleOrDefault<int>($"SELECT TOP(1) MMR FROM PlayersRatingProgress WHERE steamid = '{id}' ORDER BY DATETIME desc");
                Db.Execute($"UPDATE Players SET MMR = '{mmr}' WHERE STEAMID = '{id}'");
            }
        }
    }
}
