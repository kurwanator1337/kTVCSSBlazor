using Dapper;
using kTVCSSBlazor.Db.Interfaces;
using System.Data;
using System.Data.SqlClient;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace kTVCSSBlazor.Db.Repository
{
    public class Vips(IConfiguration configuration, ILogger logger) : Context(configuration, logger), IVips
    {
        private async Task<bool> GetInfoFromTelegram(long userId)
        {
            string token = configuration.GetValue<string>("tgBotToken");

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

        private async Task<bool> GetPremiumInfoFromTelegram(long userId)
        {
            string token = configuration.GetValue<string>("tgBotToken");

            try
            {
                TelegramBotClient botClient = new TelegramBotClient(token);
                var chat = await botClient.GetChatMemberAsync(new ChatId(-1002355396885), userId);
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

                Logger.LogInformation($"https://ktvcss.ru/player/{id} обнулил статистику");

                return true;
            }

            return false;
        }

        public async Task<bool> ResetFullStats(int id)
        {
            EnsureConnected();

            string? steam = Db.QueryFirstOrDefault<string>($"SELECT STEAMID FROM Players WHERE ID = {id}");

            if (steam != null)
            {
                if (!await IsPremiumVip(steam)) return false;

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

                Db.Execute("DeletePlayerEx", d, commandType: CommandType.StoredProcedure);

                Logger.LogInformation($"https://ktvcss.ru/player/{id} полностью обнулил статистику");

                return true;
            }

            return false;
        }

        public async Task<bool> IsPremiumVip(string steam)
        {
            EnsureConnected();

            try
            {
                long tgId = Db.QueryFirstOrDefault<long>($"SELECT TELEGRAMID FROM Players WITH (NOLOCK) WHERE STEAMID = '{steam}'");

                if (tgId > 0)
                {
                    var tg = await GetPremiumInfoFromTelegram(tgId);

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

            return false;
        }
    }
}
