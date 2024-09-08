using Dapper;
using kTVCSSBlazor.Db.Interfaces;
using System.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Data;
using System.IO.Pipelines;
using static Dapper.SqlMapper;
using System.Collections.Generic;
using kTVCSSBlazor.Components;
using kTVCSSBlazor.Data;
using MudBlazor;
using kTVCSSBlazor.Db.Models.Players;
using static MudBlazor.CategoryTypes;
using System.Runtime.Intrinsics.X86;
using System.Text;
using kTVCSSBlazor.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using kTVCSSBlazor.Db.Models.Teams;
using kTVCSSBlazor.Components.Pages.Players;
using Alert = kTVCSSBlazor.Db.Models.Players.Alert;

namespace kTVCSSBlazor.Db.Repository
{
    public class Players(IConfiguration configuration, ILogger logger) : Context(configuration, logger), IPlayers
    {
        public static MemoryCache MemoryCache = new(new MemoryCacheOptions() { });
        private string ConnectionString { get; set; } = configuration.GetConnectionString("db");

        public List<TotalPlayer> Get()
        {
            //var requester = kTVCSSHub.OnlineUsers.FirstOrDefault(x => x.Value.Id == 5940).Key;

            //kTVCSSHub.Instance.Clients.Client(requester).SendAsync("GetActionError", "test");

            MemoryCache.TryGetValue("TotalPlayerListMemory", out IEnumerable<TotalPlayer> players);

            if (players is not null)
            {
                return players.ToList();
            }

            if (System.IO.File.Exists("players.cache"))
            {
                players = JsonConvert.DeserializeObject<IEnumerable<TotalPlayer>>(System.IO.File.ReadAllText("players.cache"));

                MemoryCache.Set("TotalPlayerListMemory", players);

                return players.ToList();
            }

            EnsureConnected();

            players = Db.Query<TotalPlayer>("[kTVCSS].[dbo].[GetTotalPlayers]", commandType: CommandType.StoredProcedure, commandTimeout: 6000);

            MemoryCache.Set("TotalPlayerListMemory", players);

            System.IO.File.WriteAllText("players.cache", JsonConvert.SerializeObject(players));

            return players.ToList();
        }

        public Models.Players.Player Get(int id)
        {
            EnsureConnected();

            return Db.QueryFirstOrDefault<Models.Players.Player>($"SELECT * FROM Players WITH (NOLOCK) WHERE Id = {id}");
        }

        public Models.Players.Player Get(string steam)
        {
            EnsureConnected();

            return Db.QueryFirstOrDefault<Models.Players.Player>($"SELECT * FROM Players WITH (NOLOCK) WHERE STEAMID = '{steam}'");
        }

        public List<BannedUser> GetBannedList()
        {
            EnsureConnected();

            List<BannedUser> players = [];

            using (SqlCommand query = new($"GetBanlistBlazor", Db))
            {
                query.CommandType = CommandType.StoredProcedure;

                using SqlDataReader reader = query.ExecuteReader();
                int pos = 1;
                while (reader.Read())
                {
                    if (!players.Where(x => x.Id == int.Parse(reader[0].ToString())).Any())
                    {
                        players.Add(new BannedUser()
                        {
                            Position = pos++,
                            Id = int.Parse(reader[0].ToString()),
                            Name = reader[1].ToString(),
                            SteamID = reader[2].ToString(),
                            RankName = "/images/ranks/" + reader[3].ToString() + ".png",
                            Block = int.TryParse(reader[4]?.ToString() ?? "0", out int blk) ? blk : 0,
                            Reason = reader[5]?.ToString(),
                            PhotoUrl = reader[6]?.ToString().Length > 0 ? reader[6].ToString() : "/images/logo_ktv.png",
                            BannedBy = reader[7]?.ToString().Length > 0 ? reader[7].ToString() : "Система",
                            BanExpires = reader[8]?.ToString().Length > 0 ? reader[8].ToString() : "Навсегда",
                        });
                    }
                }
            }

            return [.. players];
        }

        public PlayerInfo GetPlayerInfo(int id)
        {
            PlayerInfo player = new();

            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();

                SqlCommand query = new($"[dbo].[GetTotalPlayerInfoById]", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                query.Parameters.AddWithValue("@id", id);

                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        double kdr = 0; string hsr = "0"; double avg = 0; string winrate = "0"; DateTime ld = new(0);
                        try
                        {
                            kdr = Math.Round(double.Parse(reader[6].ToString()), 2);
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            hsr = (Math.Round(Math.Round(double.Parse(reader[7].ToString()), 2) * 100, 2)).ToString() + "%";
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            avg = Math.Round(double.Parse(reader[9].ToString()), 2);
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            winrate = Math.Round(double.Parse(reader[17].ToString())) + "%";
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            ld = DateTime.Parse(reader[15]?.ToString());
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            player.Highlights.Tripples = int.Parse(reader[20].ToString());
                            player.Highlights.Quadros = int.Parse(reader[21].ToString());
                            player.Highlights.Aces = int.Parse(reader[22].ToString());
                            player.Highlights.OpenFrags = int.Parse(reader[23].ToString());
                        }
                        catch (Exception)
                        {

                        }
                        // PAY UR ATTENTION ON ME!!! HERE U SHOULD ADD NULL CHECKER, CUZ SOME FIELDS IN THE DB ARE NULLS
                        player.HeaderPicture = reader[27].ToString();
                        player.MainInfo.Id = id;
                        player.MainInfo.Position = int.Parse(reader[0].ToString());
                        player.MainInfo.Name = reader[1].ToString();
                        player.MainInfo.SteamID = reader[2].ToString();
                        player.MainInfo.Kills = int.Parse(reader[3].ToString());
                        player.MainInfo.Deaths = int.Parse(reader[4].ToString());
                        player.MainInfo.Headshots = int.Parse(reader[5].ToString());
                        player.MainInfo.KDR = kdr;
                        player.MainInfo.HSR = hsr;
                        player.MainInfo.MMR = int.Parse(reader[8].ToString());
                        player.MainInfo.AVG = avg;
                        player.MainInfo.RankName = reader[10].ToString();
                        player.MainInfo.MatchesTotal = int.Parse(reader[11].ToString());
                        player.MainInfo.Wons = int.Parse(reader[12].ToString());
                        player.MainInfo.Losts = int.Parse(reader[13].ToString());
                        player.MainInfo.Calibration = int.Parse(reader[14].ToString());
                        player.MainInfo.LastMatch = ld > new DateTime(2000, 1, 1) ? ld.ToString("dd.MM.yyyy HH:mm") : "еще не играл";
                        player.MainInfo.VkId = reader[16].ToString();
                        player.MainInfo.Winrate = winrate;
                        player.MainInfo.Block = int.TryParse(reader[18].ToString(), out int block) ? block : 0; // null verify??????????????
                        player.MainInfo.BlockReason = reader[19].ToString();
                        player.Name = reader[24]?.ToString() ?? "";
                        player.SureName = reader[25]?.ToString() ?? "";
                        player.MainInfo.PhotoUrl = reader[26]?.ToString().Length > 0 ? reader[26].ToString() : "/images/logo_ktv.png";
                        player.BanExpires = reader[28]?.ToString().Length > 0 ? reader[28].ToString() : "?";
                        player.MainInfo.Tier = int.Parse(reader["TIER"].ToString());
                    }
                }

                if (string.IsNullOrEmpty(player.MainInfo.SteamID))
                {
                    return player;
                }

                try
                {
                    query = new SqlCommand($"[dbo].[GetLastTwentyMatchesOfPlayer]", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@STEAMID", player.MainInfo.SteamID);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string winner = string.Empty;
                            if (int.Parse(reader[3].ToString()) > int.Parse(reader[4].ToString()))
                            {
                                winner = reader[1].ToString();
                            }
                            else
                            {
                                winner = reader[2].ToString();
                            }
                            player.LastTwentyMatches.Add(new LastTwentyMatches()
                            {
                                Result = $"{reader[1]} [{reader[3]} - {reader[4]}] {reader[2]}",
                                ID = int.Parse(reader[0].ToString()),
                                MapName = reader[6].ToString(),
                                DateTime = DateTime.Parse(reader[5].ToString()),
                                Kills = int.Parse(reader[7].ToString()),
                                Deaths = int.Parse(reader[8].ToString()),
                                Headshots = int.Parse(reader[9].ToString()),
                                Victory = reader[10].ToString().Equals(winner, StringComparison.CurrentCultureIgnoreCase)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.ToString());
                }

                query = new SqlCommand($"[dbo].[GetBestMatchesOfPlayer]", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                query.Parameters.AddWithValue("@STEAMID", player.MainInfo.SteamID);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string winner = string.Empty;
                        if (int.Parse(reader[3].ToString()) > int.Parse(reader[4].ToString()))
                        {
                            winner = reader[1].ToString();
                        }
                        else
                        {
                            winner = reader[2].ToString();
                        }
                        player.BestMatches.Add(new LastTwentyMatches()
                        {
                            Result = $"{reader[1]} [{reader[3]} - {reader[4]}] {reader[2]}",
                            ID = int.Parse(reader[0].ToString()),
                            MapName = reader[6].ToString(),
                            DateTime = DateTime.Parse(reader[5].ToString()),
                            Kills = int.Parse(reader[7].ToString()),
                            Deaths = int.Parse(reader[8].ToString()),
                            Headshots = int.Parse(reader[9].ToString()),
                            Victory = reader[10].ToString().Equals(winner, StringComparison.CurrentCultureIgnoreCase)
                        });
                    }
                }

                query = new SqlCommand($"[dbo].[GetRankProgressPlayer]", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                query.Parameters.AddWithValue("@STEAMID", player.MainInfo.SteamID);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        player.Rating.Add(new Models.Players.Rating()
                        {
                            Points = int.Parse(reader[0].ToString()),
                            DateTime = DateTime.Parse(reader[1].ToString())
                        });
                    }
                }

                query = new SqlCommand($"[dbo].[GetWeaponsByPlayer]", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                query.Parameters.AddWithValue("@STEAMID", player.MainInfo.SteamID);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        player.Weapons.Add(new Weapons()
                        {
                            Count = int.Parse(reader[1].ToString()),
                            Weapon = reader[0].ToString()
                        });
                    }
                }

                query = new SqlCommand($"[dbo].[GetPlayerTopInfo]", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                query.Parameters.AddWithValue("@STEAMID", player.MainInfo.SteamID);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        player.TopMap = reader[0].ToString();
                        player.TopWeapon = reader[1].ToString();
                    }
                }

                query = new SqlCommand($"SELECT DISTINCT NAME FROM MatchesResults WITH (NOLOCK) WHERE STEAMID = '{player.MainInfo.SteamID}'", connection);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        player.NicknameHistory.Add(new Nick() { Name = reader[0].ToString() });
                    }
                }

                if (player.MainInfo.SteamID != "STEAM_UNDEFINED")
                {
                    query = new SqlCommand($"SELECT Teams.NAME, Teams.ID FROM TeamsMembers AS TM WITH (NOLOCK) INNER JOIN Teams WITH (NOLOCK) ON TM.TEAMID = Teams.ID WHERE STEAMID = '{player.MainInfo.SteamID}'", connection);
                    using SqlDataReader reader = query.ExecuteReader();
                    while (reader.Read())
                    {
                        player.TeamInfo.Name = reader[0].ToString();
                        player.TeamInfo.ID = reader[1].ToString();
                    }
                }

                string vip = connection.QueryFirstOrDefault<string>($"SELECT STEAMID FROM Vips WITH (NOLOCK) WHERE STEAMID = '{player.MainInfo.SteamID}'");

                if (!string.IsNullOrEmpty(vip))
                {
                    player.Vip = true;
                }
                else
                {
                    vip = connection.QueryFirstOrDefault<string>($"SELECT NAME FROM Admins WITH (NOLOCK) WHERE NAME = '{player.MainInfo.SteamID}'");

                    if (!string.IsNullOrEmpty(vip))
                    {
                        player.Vip = true;
                    }
                }

                player.Friends.AddRange(connection.Query<Friend>($"SELECT (CASE WHEN Players.LOGIN IS NULL THEN Players.NAME ELSE Players.LOGIN END) AS Name, Players.ID AS ID, Players.PHOTO as Photo FROM Players WITH (NOLOCK) INNER JOIN PlayersFriends WITH (NOLOCK) ON Players.ID = PlayersFriends.FID WHERE PlayersFriends.ID = {id}"));
                player.Friends.AddRange(connection.Query<Friend>($"SELECT (CASE WHEN Players.LOGIN IS NULL THEN Players.NAME ELSE Players.LOGIN END) AS Name, Players.ID AS ID, Players.PHOTO as Photo FROM Players WITH (NOLOCK) INNER JOIN PlayersFriends WITH (NOLOCK) ON Players.ID = PlayersFriends.ID WHERE PlayersFriends.FID = {id}"));
                player.TeamMates.AddRange(connection.Query<Friend>($"SELECT (CASE WHEN P.LOGIN IS NULL THEN P.NAME ELSE P.LOGIN END) AS Name, P.ID AS ID, P.PHOTO as Photo FROM Players AS P WITH (NOLOCK) LEFT JOIN TeamsMembers AS TM WITH (NOLOCK) ON P.STEAMID = TM.STEAMID LEFT JOIN Teams AS T WITH (NOLOCK) ON T.CAPTAINSTEAMID = P.STEAMID WHERE TEAMID = (SELECT TEAMID FROM TeamsMembers WHERE STEAMID = (SELECT STEAMID FROM Players WHERE ID = {id})) OR T.ID = (SELECT TEAMID FROM TeamsMembers WHERE STEAMID = (SELECT STEAMID FROM Players WHERE ID = {id}))"));
                player.Description = connection.QueryFirstOrDefault<string>($"SELECT DESCRIPTION FROM Players WITH (NOLOCK) WHERE ID = {id}");
                player.Ranks.AddRange(connection.Query<Rank>("SELECT NAME as Name, STARTMMR as Min, ENDMMR as Max FROM Ranks WITH (NOLOCK) WHERE NAME != 'UNRANKED'"));

                player.AvgInfo.AvgKills = Math.Round(player.LastTwentyMatches.Select(x => x.Kills).Sum() / (double)player.LastTwentyMatches.Count, 2);
                player.AvgInfo.AvgDeaths = Math.Round(player.LastTwentyMatches.Select(x => x.Deaths).Sum() / (double)player.LastTwentyMatches.Count, 2);
                player.AvgInfo.AvgHeadshots = Math.Round(player.LastTwentyMatches.Select(x => x.Headshots).Sum() / (double)player.LastTwentyMatches.Count, 2);
                player.AvgInfo.AvgKdr = Math.Round(player.AvgInfo.AvgKills / (double)player.AvgInfo.AvgDeaths, 2);
                player.AvgInfo.AvgHsr = Math.Round(Math.Round(player.AvgInfo.AvgHeadshots / (double)player.AvgInfo.AvgKills, 2) * 100, 2) + "%";

                player.AvgInfo.AvgKills = player.AvgInfo.AvgKills.Equals(double.NaN) ? 0 : player.AvgInfo.AvgKills;
                player.AvgInfo.AvgDeaths = player.AvgInfo.AvgDeaths.Equals(double.NaN) ? 0 : player.AvgInfo.AvgDeaths;
                player.AvgInfo.AvgHeadshots = player.AvgInfo.AvgHeadshots.Equals(double.NaN) ? 0 : player.AvgInfo.AvgHeadshots;
                player.AvgInfo.AvgKdr = player.AvgInfo.AvgKdr.Equals(double.NaN) ? 0 : player.AvgInfo.AvgKdr;

                player.RankUrl = "/images/ranks/" + player.MainInfo.RankName + ".png";

                if (player.LastTwentyMatches.Count >= 5)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (player.LastTwentyMatches[i].Victory)
                        {
                            player.LastFiveMatches += "<span style=\"color: lime\">W</span>";
                        }
                        else
                        {
                            player.LastFiveMatches += "<span style=\"color: red\">L</span>";
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < player.LastTwentyMatches.Count; i++)
                    {
                        if (player.LastTwentyMatches[i].Victory)
                        {
                            player.LastFiveMatches += "<span style=\"color: lime\">W</span>";
                        }
                        else
                        {
                            player.LastFiveMatches += "<span style=\"color: red\">L</span>";
                        }
                    }
                }

                if ((double)player.Highlights?.OpenFrags / (double)player.MainInfo?.Kills >= 0.15)
                {
                    player.Role = "Энтрифрагер"; // +w dolbaeb
                }
                else if ("awp scout".Contains(player.TopWeapon))
                {
                    player.Role = "Снайпер";
                }
                else if ("deagle".Contains(player.TopWeapon))
                {
                    player.Role = "Диглер";
                }
                else
                {
                    if (player.MainInfo.Kills > 0)
                    {
                        player.Role = "Рифлер";
                    }
                }
            }

            var ach = Db.Query($"SELECT * FROM [PlayersAchievements] WHERE ID = {id}");
            player.NormalAchiviments = Db.Query<Achiviment>($"SELECT * FROM [PlayersAchievements] WHERE ID = {id}").ToList();

            if (ach.Any())
            {
                StringBuilder sb = new();

                foreach (var a in ach)
                {
                    if (a.PLACE == 1)
                    {
                        sb.Append($"<div>🥇 {a.NAME}</div><br />");
                    }
                    if (a.PLACE == 2)
                    {
                        sb.Append($"<div>🥈 {a.NAME}</div><br />");
                    }
                    if (a.PLACE == 3)
                    {
                        sb.Append($"<div>🥉 {a.NAME}</div><br />");
                    }
                }

                player.Achiviments = sb.ToString();
            }

            DynamicParameters d = new DynamicParameters();
            d.Add("STEAM", player.MainInfo.SteamID);

            player.MainInfo.GameHours = Db.QueryFirstOrDefault<string>("GetGameHoursBySteam", d, commandType: CommandType.StoredProcedure);

            player.ProjectRating = Math.Pow((double)(0.1 * player.AvgInfo.AvgKdr + 0.2 * player.MainInfo.AVG + 0.05 * player.AvgInfo.AvgHeadshots + 0.125 * (1 - player.AvgInfo.AvgDeaths) + 0.05 * (player.MainInfo.Wons / player.MainInfo.Losts)), 0.6);

            player.Social = Db.QueryFirstOrDefault<Social>($"SELECT TELEGRAMID, VKID, TwitchUrl, YoutubeUrl FROM Players WHERE ID = '{id}'");

            if (long.TryParse(player.Social.VkId, out long vkid))
            {
                player.Social.VkId = $"https://vk.com/id{vkid}";
            }

            player.BanMultiplier = Db.QuerySingleOrDefault<Behavior>($"SELECT CASE  WHEN [BANMULTIPLIER] IS NULL THEN 0  ELSE [BANMULTIPLIER]  END AS BANMULTIPLIER FROM [kTVCSS].[dbo].[Players] WHERE ID = {id}");

            player.Chat =
                Db.Query<ChatHistory>(
                    $"SELECT * FROM [kTVCSS].[dbo].[ChatHistory] WITH (NOLOCK) WHERE STEAMID = '{player.MainInfo.SteamID}' ORDER BY DATETIME DESC").ToList();

            return player;
        }

        public Profile GetProfile(int id)
        {
            EnsureConnected();

            return Db.QueryFirst<Profile>($"SELECT LOGIN AS Login, PASSWORD AS Password, PHOTO AS AvatarUrl, HeaderPicture AS HeaderUrl, DESCRIPTION AS Description, NEEDTEAM as NeedTeam, PreferredRole, LastTeam, PrimeTime, StartPlayYear, Microphone, TeamSpeak, Discord, TELEGRAMID as Telegram, VKID, YoutubeUrl, TwitchUrl FROM Players AS P WITH (NOLOCK) LEFT JOIN PlayersFFT AS F WITH (NOLOCK) ON P.ID = F.ID WHERE P.ID = {id}");
        }

        public string SaveProfile(Profile profile, int id)
        {
            string result = "OK";

            EnsureConnected();

            try
            {
                if (profile.Telegram is not null)
                {
                    var tg = Db.QueryFirstOrDefault($"SELECT TOP(1) TELEGRAMID, ID FROM Players WHERE TELEGRAMID = {profile.Telegram}");

                    if (!string.IsNullOrEmpty(tg.TELEGRAMID.ToString()))
                    {
                        if (tg.ID != id)
                        {
                            if (tg.ID != "0")
                            {
                                result = "Этот телеграм уже был кем-то привязан!";
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //
            }

            try
            {
                if (profile.Password.Length != 32)
                {
                    profile.Password = Tools.CreateMD5(profile.Password);
                }

                DynamicParameters p = new();

                p.Add("Login", profile.Login);
                p.Add("Password", profile.Password);
                p.Add("AvatarUrl", profile.AvatarUrl);
                p.Add("HeaderUrl", profile.HeaderUrl);
                p.Add("Description", profile.Description);
                p.Add("NeedTeam", profile.NeedTeam ? 1 : 0);
                p.Add("PreferredRole", profile.PreferredRole);
                p.Add("LastTeam", profile.LastTeam);
                p.Add("PrimeTime", profile.PrimeTime);
                p.Add("StartPlayYear", profile.StartPlayYear);
                p.Add("Microphone", profile.Microphone ? 1 : 0);
                p.Add("TeamSpeak", profile.TeamSpeak ? 1 : 0);
                p.Add("Discord", profile.Discord ? 1 : 0);
                p.Add("ID", id);
                p.Add("Telegram", profile.Telegram);
                p.Add("VK", profile.VkId);
                p.Add("YT", profile.YoutubeUrl);
                p.Add("TWITCH", profile.TwitchUrl);

                Db.Execute("UpdateProfile", p, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            return result;
        }

        public List<Models.Players.Fft> GetFftList()
        {
            EnsureConnected();

            return Db.Query<Models.Players.Fft>("SELECT P.ID, (CASE WHEN LOGIN IS NULL THEN NAME ELSE LOGIN END) AS Name, CONCAT('/images/ranks/', RANKNAME, '.png') as RankName, ROUND(CASE WHEN KDR IS NULL THEN 0 ELSE KDR END, 2) AS KDR, ROUND(CASE WHEN HSR IS NULL THEN 0 ELSE HSR END, 2) AS HSR, ROUND(CASE WHEN AVG IS NULL THEN 0 ELSE AVG END, 2) AS AVG, CAST(MATCHESPLAYED AS INT) AS MatchesTotal, CONCAT(ROUND(CASE WHEN WINRATE IS NULL THEN 0 ELSE WINRATE END, 2), '%') AS Winrate, PreferredRole, LastTeam, PrimeTime, StartPlayYear, Microphone, TeamSpeak, Discord FROM Players AS P WITH (NOLOCK) INNER JOIN PlayersFFT AS F WITH (NOLOCK) ON P.ID = F.ID").ToList();
        }

        public bool AddFriend(int player, int target)
        {
            EnsureConnected();

            var exist = Db.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM PlayersFriendsRequests WHERE ID = {player} AND TARGET = {target}");

            if (exist > 0)
            {
                return false;
            }

            Db.Execute($"INSERT INTO PlayersFriendsRequests VALUES ({player}, {target})");

            var targetPlayer = kTVCSSHub.OnlineUsers.FirstOrDefault(x => x.Value.Id == target);
            var sourcePlayer = kTVCSSHub.OnlineUsers.FirstOrDefault(x => x.Value.Id == player);

            if (targetPlayer.Key != null)
            {
                if (sourcePlayer.Key != null)
                {
                    if (!kTVCSSHub.SearchUsers.Where(x => x.Key == targetPlayer.Key).Any())
                    {
                        kTVCSSHub.Instance.Clients.Client(targetPlayer.Key).SendAsync("NewFriendRequest", sourcePlayer.Value, target);
                    }
                }
            }

            return true;
        }

        public void RemoveFriend(int player, int target)
        {
            EnsureConnected();

            Db.Execute($"DELETE FROM PlayersFriends WHERE (ID = {player} AND FID = {target}) OR (FID = {player} AND ID = {target})");

            RefreshFriendsUsers(); RefreshOnlineUsers();
        }

        public async Task RefreshOnlineUsers()
        {
            foreach (var player in kTVCSSHub.OnlinePageUsers)
            {
                try
                {
                    await kTVCSSHub.Instance.Clients.Client(player.Key).SendAsync("RefreshOnline");
                }
                catch (Exception)
                {
                    // игрока нет в сети
                }
            }
        }

        public async Task RefreshFriendsUsers()
        {
            foreach (var player in kTVCSSHub.FriendsPageUsers)
            {
                try
                {
                    await kTVCSSHub.Instance.Clients.Client(player.Key).SendAsync("RefreshFriends");
                }
                catch (Exception)
                {
                    // не в сети
                }
            }
        }

        public List<FriendRequest> GetFriendRequests(int player)
        {
            EnsureConnected();

            try
            {
                var requests = Db.Query<FriendRequest>($"SELECT P.ID as Id, (CASE WHEN P.LOGIN IS NULL THEN P.NAME ELSE P.LOGIN END) as Name FROM PlayersFriendsRequests AS F WITH (NOLOCK) INNER JOIN " +
                $"Players AS P WITH (NOLOCK) ON P.ID = F.ID WHERE F.TARGET = {player}");

                return requests.DistinctBy(x => x.Id).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                return new List<FriendRequest>();
            }
        }

        public void AcceptFriendRequest(int player, int target)
        {
            EnsureConnected();

            Db.Execute($"INSERT INTO PlayersFriends VALUES ({player}, {target})");

            RemoveFriendRequest(player, target);

            RefreshFriendsUsers(); RefreshOnlineUsers();
        }

        public void RemoveFriendRequest(int player, int target)
        {
            EnsureConnected();

            Db.Execute($"DELETE FROM PlayersFriendsRequests WHERE ID = {target} AND TARGET = {player}");

            RefreshFriendsUsers(); RefreshOnlineUsers();
        }

        public bool IsFriend(int player, int target)
        {
            EnsureConnected();

            var question = Db.Query($"SELECT * FROM PlayersFriends WITH (NOLOCK) WHERE ID = {player} AND FID = {target}");

            if (question.Any()) return true;
            else
            {
                question = Db.Query($"SELECT * FROM PlayersFriends WITH (NOLOCK) WHERE FID = {player} AND ID = {target}");
                if (question.Any()) return true;
                else return false;
            }
        }

        public List<TotalPlayer> GetFriends(int player)
        {
            EnsureConnected();

            List<TotalPlayer> friends = [];

            var all = Get();

            try
            {
                var listIds = Db.Query<int>($"SELECT FID FROM PlayersFriends WITH (NOLOCK) WHERE ID = {player}").ToList();

                listIds.AddRange(Db.Query<int>($"SELECT ID FROM PlayersFriends WITH (NOLOCK) WHERE FID = {player}"));

                listIds = listIds.Distinct().ToList();

                foreach (var id in listIds)
                {
                    friends.Add(all.FirstOrDefault(x => x.Id == id));
                }

                friends.RemoveAll(x => x == null);

                foreach (var friend in friends)
                {
                    if (kTVCSSHub.OnlineUsers.Where(x => x.Value.Id == friend.Id).Any())
                    {
                        friend.IsOnline = true;
                    }
                    else friend.IsOnline = false;
                }

                return friends.OrderByDescending(x => x.IsOnline).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                return friends;
            }
        }

        public List<FriendRequest> GetFriendRequestsOutgoing(int player)
        {
            EnsureConnected();

            try
            {
                var requests = Db.Query<FriendRequest>($"SELECT P.ID as Id, (CASE WHEN P.LOGIN IS NULL THEN P.NAME ELSE P.LOGIN END) as Name FROM PlayersFriendsRequests AS F WITH (NOLOCK) INNER JOIN " +
                $"Players AS P WITH (NOLOCK) ON P.ID = F.TARGET WHERE F.ID = {player}");

                return requests.DistinctBy(x => x.Id).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                return new List<FriendRequest>();
            }
        }

        public void MakeReport(int me, int player, string text)
        {
            EnsureConnected();

            DynamicParameters d = new();
            d.Add("FROMID", me);
            d.Add("PLAYERID", player);
            d.Add("TEXT", text);

            Logger.LogInformation($"https://ktvcss.ru/player/{me} подал жалобу на игрока https://ktvcss.ru/player/{player} - {text}");

            Db.Execute("MakeReport", d, commandType: CommandType.StoredProcedure);
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

            EnsureConnected();

            try
            {
                string vip = Db.QueryFirstOrDefault<string>($"SELECT NAME FROM Admins WITH (NOLOCK) WHERE NAME = '{steam}'");

                if (!string.IsNullOrEmpty(vip))
                {
                    return true;
                }
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetAfterSignupMessage(int id)
        {
            EnsureConnected();

            string code = Db.QueryFirstOrDefault<string>($"SELECT CONFIRMCODE FROM Players WHERE ID = {id}");

            return code;
        }

        public List<TotalPlayer> GetOnlinePlayers()
        {
            List<TotalPlayer> online = [];

            var all = Get();

            var onlineIds = all.Select(x => x.Id).ToList();

            var intersect = onlineIds.Intersect(kTVCSSHub.OnlineUsers.Select(x => x.Value.Id));

            var o = all.IntersectBy(intersect, x => x.Id).ToList();

            foreach (var i in o)
            {
                if (kTVCSSHub.OnlineUsers.Where(x => x.Value.Id == i.Id).Any())
                {
                    i.IsOnline = true;
                }
                else i.IsOnline = false;
            }

            return o;
        }

        public string GetTelegramID(int id)
        {
            EnsureConnected();

            return Db.QueryFirstOrDefault<string>($"SELECT TELEGRAMID FROM Players WHERE ID = {id}");
        }

        public void SavePlayerAchiviments(int id, string achvs)
        {
            List<Achiviment> achiviments = new List<Achiviment>();

            if (achvs != null)
            {
                if (achvs.Length > 0)
                {
                    var list = achvs.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    EnsureConnected();

                    foreach (var item in list)
                    {
                        var k = item.Split('-');
                        achiviments.Add(new Achiviment()
                        {
                            Place = int.Parse(k[0]),
                            Name = k[1]
                        });
                    }

                    Db.Execute($"DELETE FROM PlayersAchievements WHERE ID = {id}");

                    foreach (var ach in achiviments)
                    {
                        Db.Execute($"INSERT INTO PlayersAchievements VALUES ({id}, '{ach.Name}', {ach.Place})");
                    }
                }
                else
                {
                    Db.Execute($"DELETE FROM PlayersAchievements WHERE ID = {id}");
                }
            }
        }

        public void SetLCReaded(int id)
        {
            EnsureConnected();

            Db.Execute($"UPDATE Players SET LICENSEREADED = 1 WHERE ID = {id}");
        }

        public async Task<List<Alert>> GetAlerts(int id)
        {
            EnsureConnected();
            
            var t = await Db.QueryAsync<Alert>($"SELECT * FROM Alerts WHERE ID = {id} AND Viewed = 0");

            return t.ToList();
        }

        public void SupressAlerts(int id)
        {
            EnsureConnected();
            
            Db.Execute($"UPDATE Alerts SET Viewed = 1 WHERE ID = {id}");
        }

        public bool HasCam(int id)
        {
            EnsureConnected();

            try
            {
                int cam = Db.QuerySingleOrDefault<int>($"SELECT CAMREQUIRED FROM Players WHERE ID = {id}");

                return cam == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        List<Cam> IPlayers.GetCamHistory(int id)
        {
            EnsureConnected();

            return Db.Query<Cam>($"SELECT * FROM CamHistory WHERE ID = {id}").ToList();
        }

        public void SaveCam(Cam cam)
        {
            EnsureConnected();

            Db.Execute($"UPDATE CamHistory SET VideoURL = '{cam.VideoUrl}' WHERE MATCHID = {cam.MatchId} AND ID = {cam.Id}");
        }
    }
}
