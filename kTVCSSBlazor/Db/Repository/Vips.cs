using Dapper;
using kTVCSSBlazor.Db.Interfaces;
using System.Data;
using System.Data.SqlClient;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace kTVCSSBlazor.Db.Repository
{
    public class Vips(string connectionString) : IVips
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

        private async Task<bool> GetInfoFromTelegram(long userId)
        {
            string token = "7283485794:AAHIPhfiZI5ZkCjfr3B0lFyvlts19NX7yrE";

            try
            {
                TelegramBotClient botClient = new TelegramBotClient(token);
                var chat = await botClient.GetChatMemberAsync(new ChatId(-1002154538468), userId);
                if (chat.User != null)
                {
                    if (chat.Status == ChatMemberStatus.Member || chat.Status == ChatMemberStatus.Restricted)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public async Task<bool> IsVip(string steam)
        {
            EnsureConnected();

            try
            {
                long tgId = Db.QueryFirstOrDefault<long>($"SELECT TELEGRAMID FROM Players WITH (NOLOCK) WHERE STEAMID = '{steam}'");

                if (tgId > 0)
                {
                    var tg = await GetInfoFromTelegram(tgId);

                    if (tg)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // tg null
            }

            string vip = Db.QueryFirstOrDefault<string>($"SELECT NAME FROM Admins WITH (NOLOCK) WHERE NAME = '{steam}'");

            if (!string.IsNullOrEmpty(vip))
            {
                return true;
            }
            else return false;
        }

        public async Task<bool> ResetStats(int id)
        {
            EnsureConnected();

            string? steam = Db.QueryFirstOrDefault<string>($"SELECT STEAMID FROM Players WHERE ID = {id}");

            if (steam != null)
            {
                if (!await IsVip(steam)) return false;

                int matchesCount = Db.QueryFirstOrDefault<int>($"SELECT MATCHESPLAYED FROM Players WHERE ID = {id}");

                if (matchesCount > 0) 
                {
                    if (matchesCount < 10)
                    {
                        return false;
                    }
                }

                DynamicParameters d = new();
                d.Add("STEAMID", steam);

                Db.Execute("DeletePlayer", d, commandType: CommandType.StoredProcedure);

                return true;
            }

            return false;
        }
    }
}
