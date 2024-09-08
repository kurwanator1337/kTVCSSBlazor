using kTVCSSBlazor.Db.Interfaces;
using System.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using kTVCSSBlazor.Db.Models.Players;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace kTVCSSBlazor.Db.Repository
{
    public class Moderators(IConfiguration configuration, ILogger logger) : Context(configuration, logger), IModerators
    {
        public async Task<bool> IsModerator(int id)
        {
            EnsureConnected();

            int result = await Db.QueryFirstOrDefaultAsync<int>($"SELECT MODERATOR FROM Players WHERE ID = {id}");

            return result == 1;
        }

        public async Task SetTier(string moderator, int id, int tier)
        {
            EnsureConnected();

            Db.ExecuteAsync($"UPDATE Players SET TIER = {tier} WHERE ID = {id}");

            kTVCSSBlazor.Db.Repository.Players.MemoryCache.TryGetValue("TotalPlayerListMemory", out IEnumerable<TotalPlayer> players);

            if (players is not null)
            {
                players.FirstOrDefault(x => x.Id == id).Tier = tier;

                kTVCSSBlazor.Db.Repository.Players.MemoryCache.Set("TotalPlayerListMemory", players);

                try
                {
                    System.IO.File.WriteAllText("players.cache", JsonConvert.SerializeObject(players));
                }
                catch (Exception)
                {
                    // file busy
                }

                Logger.LogInformation($"{moderator} установил тир {tier} игроку {id}");
            }
        }
    }
}
