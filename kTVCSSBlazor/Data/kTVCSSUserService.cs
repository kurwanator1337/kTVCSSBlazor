using Dapper;
using kTVCSSBlazor.Db;
using kTVCSSBlazor.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Security.Claims;

namespace kTVCSSBlazor.Data
{
    public class kTVCSSUserService
    {
        private readonly ProtectedLocalStorage _protectedLocalStorage;
        private readonly string _kTVStorageKey = "kTVCSSIdentityv2";
        private IConfiguration configuration;

        public kTVCSSUserService(ProtectedLocalStorage protectedLocalStorage, IConfiguration configuration)
        {
            _protectedLocalStorage = protectedLocalStorage;
            this.configuration = configuration;
        }

        private List<string> GetRoles(string steam)
        {
            var roles = new List<string>();

            using (var db = new SqlConnection(configuration.GetConnectionString("db")))
            {
                db.Open();

                string exist = db.QueryFirstOrDefault<string>($"SELECT NAME FROM Admins WHERE NAME = '{steam}'");

                if (!string.IsNullOrEmpty(exist))
                {
                    roles.Add("Admin");
                }

                string vip = db.QueryFirstOrDefault<string>($"SELECT STEAMID FROM Vips WHERE STEAMID = '{steam}'");

                if (!string.IsNullOrEmpty(vip))
                {
                    roles.Add("Vip");
                }
            }

            if (roles.Count == 0) roles.Add("User");

            return roles;
        }

        public async Task<string> Register(string username, string password)
        {
            string md5 = Tools.CreateMD5(password);

            DynamicParameters dynamicParameters = new();
            dynamicParameters.Add("NAME", username);
            dynamicParameters.Add("PASSWORD", md5);

            using (var db = new SqlConnection(configuration.GetConnectionString("db")))
            {
                db.Open();
                return await db.QueryFirstOrDefaultAsync<string>("[IReg]", commandType: System.Data.CommandType.StoredProcedure, param: dynamicParameters);
            }
        }

        public async Task<User?> LookupUserInDatabaseAsync(string username, string password)
        {
            string md5 = Tools.CreateMD5(password);

            DynamicParameters dynamicParameters = new();
            dynamicParameters.Add("NAME", username);
            dynamicParameters.Add("PASSWORD", md5);

            using (var db = new SqlConnection(configuration.GetConnectionString("db")))
            {
                db.Open();

                UserCookie result = await db.QueryFirstOrDefaultAsync<UserCookie>("[IAuth]", commandType: System.Data.CommandType.StoredProcedure, param: dynamicParameters);

                if (result != null)
                {
                    User user = new()
                    {
                        Username = username,
                        Password = password,
                        RankPicture = result.RankPicture,
                        CurrentMMR = result.CurrentMMR,
                        TeamID = result.TeamID,
                        AvatarUrl = result.AvatarUrl,
                        VkUid = result.VkUid,
                        MinMMR = result.MinMMR,
                        MaxMMR = result.MaxMMR,
                        SteamId = result.SteamId,
                        TeamName = result.TeamName,
                        TeamPicture = result.TeamPicture,
                        TotalMatches = result.TotalMatches,
                        Roles = GetRoles(result.SteamId),
                        Id = result.Id,
                        Tier = result.Tier
                    };

                    return user;
                }
            }

            return null;
        }

        public async Task PersistUserToBrowserAsync(User user)
        {
            string userJson = JsonConvert.SerializeObject(user);
            await _protectedLocalStorage.SetAsync(_kTVStorageKey, userJson);
        }

        public async Task<User?> FetchUserFromBrowserAsync()
        {
            try
            {
                var storedUserResult = await _protectedLocalStorage.GetAsync<string>(_kTVStorageKey);

                if (storedUserResult.Success && !string.IsNullOrEmpty(storedUserResult.Value))
                {
                    var user = JsonConvert.DeserializeObject<User>(storedUserResult.Value);

                    return user;
                }
            }
            catch (InvalidOperationException)
            {
            }

            return null;
        }

        public async Task ClearBrowserUserDataAsync() => await _protectedLocalStorage.DeleteAsync(_kTVStorageKey);
    }
}