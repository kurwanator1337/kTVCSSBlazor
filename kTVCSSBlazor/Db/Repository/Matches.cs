using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Models.Matches;
using Microsoft.Extensions.Caching.Memory;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using kTVCSSBlazor.Db.Models.Players;
using static Dapper.SqlMapper;
using System.Text.Json.Serialization;
using System.Text.Json;
using kTVCSSBlazor.Models;

namespace kTVCSSBlazor.Db.Repository
{
    public class Matches(string connectionString) : IMatches
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

        public List<TotalMatch> GetTotalMatches()
        {
            EnsureConnected();

            List<TotalMatchEx2> liveMatches = [];
            List<TotalMatchEx2> matches = [];

            using (SqlCommand query = new($"GetListLiveMatches", Db))
            {
                query.CommandType = CommandType.StoredProcedure;

                using SqlDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    TotalMatchEx2 match = new()
                    {
                        Id = int.Parse(reader[0].ToString()),
                        MapName = reader[4].ToString(),
                        MatchDate = "LIVE",
                        ServerId = int.Parse(reader[3].ToString())
                    };
                    string aScore = reader[1].ToString();
                    string bScore = reader[2].ToString();
                    if (aScore.Length == 1)
                    {
                        aScore = "0" + aScore;
                    }
                    if (bScore.Length == 1)
                    {
                        bScore = "0" + bScore;
                    }
                    match.Score = aScore + " - " + bScore;
                    match.Name = reader[5].ToString();
                    match.SteamID = reader[6].ToString();
                    match.Team = reader[10].ToString();
                    matches.Add(match);
                }
            }

            var distinct = matches.DistinctBy(x => x.Id);
            foreach (var match in distinct)
            {
                TotalMatchEx2 matchEx = new()
                {
                    Id = match.Id,
                    ServerId = match.ServerId,
                    Score = match.Score,
                    MatchDate = match.MatchDate,
                    MapName = match.MapName
                };

                var tags = (from p in matches.Where(x => x.Id == match.Id) select new Models.Matches.Player { Name = p.Name, Team = p.Team }).ToList();
                var names = GetTeamNames(tags);
                matchEx.ATeam = names["TERRORIST"];
                matchEx.BTeam = names["CT"];

                liveMatches.Add(matchEx);
            }

            List<TotalMatch> dataSource = [];

            foreach (var live in liveMatches)
            {
                dataSource.Add(live);
            }

            dataSource.AddRange(Db.Query<TotalMatch>("GetTotalMatches", commandType: CommandType.StoredProcedure));

            return dataSource;
        }

        private Dictionary<string, string> GetTeamNames(List<Models.Matches.Player> players)
        {
            Dictionary<string, string> tags = new()
            {
                { "TERRORIST", "TERRORIST" },
                { "CT", "CT" }
            };

            try
            {
                List<string> ctTags = [];
                List<string> terTags = [];

                IEnumerable<Models.Matches.Player> ctPlayers = players.Where(x => x.Team == "CT");
                foreach (var player in ctPlayers)
                {
                    ctTags.Add(player.Name.Split(' ')[0]);
                }

                IEnumerable<Models.Matches.Player> terPlayers = players.Where(x => x.Team == "TERRORIST");
                foreach (var player in terPlayers)
                {
                    terTags.Add(player.Name.Split(' ')[0]);
                }

                tags["CT"] = "Team " + ctTags[0];
                tags["TERRORIST"] = "Team " + terTags[0];

                foreach (var possibleTag in ctTags)
                {
                    if (ctTags.Count(x => x == possibleTag) >= 2)
                    {
                        tags["CT"] = possibleTag;
                        break;
                    }
                }

                foreach (var possibleTag in terTags)
                {
                    if (terTags.Count(x => x == possibleTag) >= 2)
                    {
                        tags["TERRORIST"] = possibleTag;
                        break;
                    }
                }

                return tags;
            }
            catch (Exception ex)
            {
                return tags;
            }
        }

        public List<TotalMatch> GetMyBestMatches(string steam)
        {
            EnsureConnected();

            List<TotalMatch> matches = [];

            var query = new SqlCommand($"[dbo].[GetBestMatchesOfPlayer]", Db)
            {
                CommandType = CommandType.StoredProcedure
            };
            query.Parameters.AddWithValue("@STEAMID", steam);
            using (SqlDataReader reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    matches.Add(new TotalMatch()
                    {
                        ATeam = reader[1].ToString(),
                        BTeam = reader[2].ToString(),
                        Id = Convert.ToInt32(reader[0].ToString()),
                        MapName = reader[6].ToString(),
                        MatchDate = DateTime.Parse(reader[5].ToString()).ToString("dd.MM.yyyy HH:mm"),
                        ServerId = 0,
                        Score = $"{reader[3]} - {reader[4]}"
                    });
                }
            }

            return matches;
        }

        public MatchInfo GetMatchByID(int id)
        {
            MatchInfo info = new();
            bool isLive = false;

            EnsureConnected();

            SqlCommand query = new($"SELECT [ANAME], [BNAME], [ASCORE], [BSCORE], [MAP], [SERVERID] FROM [dbo].[Matches] WITH (NOLOCK) WHERE ID = {id}", Db);
            using (SqlDataReader reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    info.AName = reader[0].ToString();
                    info.BName = reader[1].ToString();
                    info.AScore = double.Parse(reader[2].ToString());
                    info.BScore = double.Parse(reader[3].ToString());
                    info.MapName = reader[4].ToString();
                    info.ServerName = "kTVCSS #" + reader[5].ToString();
                }
            }

            if (string.IsNullOrEmpty(info.AName))
            {
                isLive = true;
            }

            if (!isLive)
            {
                info.IsFinished = true;
                try
                {
                    #region Это не лайв матч 


                    query = new SqlCommand($"SELECT (CASE WHEN LOGIN IS NULL THEN p.NAME ELSE LOGIN END), mr.[STEAMID], mr.[KILLS], mr.[DEATHS], mr.[HEADSHOTS], p.RANKNAME, " +
                        $"[OPENFRAGS], [TRIPPLES], [QUADROS], [RAMPAGES], p.VKID, p.ID, p.MMR, p.PHOTO FROM [dbo].[MatchesResults] as mr WITH (NOLOCK) INNER JOIN [dbo].[Players] as p WITH (NOLOCK) ON p.STEAMID = mr.STEAMID " +
                        $"INNER JOIN MatchesHighlights as mh WITH (NOLOCK) ON mh.STEAMID = mr.STEAMID WHERE mh.ID = {id} and mr.ID = {id} and mr.TEAMNAME = '{info.AName}'", Db);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            info.AMPlayers.Add(new MatchInfo.MPlayerInfo()
                            {
                                Name = reader[0].ToString(),
                                DisplayName = reader[0].ToString(),
                                SteamID = reader[1].ToString(),
                                Url = "/images/ranks/" + reader[5].ToString() + ".png",
                                VkID = reader[10].ToString(),
                                ID = reader[11].ToString(),
                                AvatarUrl = reader[13].ToString().Length > 0 ? reader[13].ToString() : "/images/logo_ktv.png"
                            });

                            info.ATeamMMR.Add(double.Parse(reader[12].ToString()));

                            var gridItem = new MatchInfo.MatchStatsGrid()
                            {
                                Name = reader[0].ToString(),
                                SteamID = reader[1].ToString(),
                                Kills = double.Parse(reader[2].ToString()),
                                Deaths = double.Parse(reader[3].ToString()),
                                Headshots = double.Parse(reader[4].ToString()),
                                PictureUrl = "/images/ranks/" + reader[5].ToString() + ".png",
                                OpenFrags = int.Parse(reader[6].ToString()),
                                MMR = int.Parse(reader["MMR"].ToString()),
                                Triples = int.Parse(reader[7].ToString()),
                                Quadros = int.Parse(reader[8].ToString()),
                                Aces = int.Parse(reader[9].ToString()),
                                VkID = reader[10].ToString(),
                                ID = reader[11].ToString(),
                                AvatarUrl = reader[13].ToString().Length > 0 ? reader[13].ToString() : "/images/logo_ktv.png"
                            };

                            gridItem.KRR = Math.Round(gridItem.Kills / (info.AScore + info.BScore), 2) >= 0 ? Math.Round(gridItem.Kills / (info.AScore + info.BScore), 2) : 0;
                            gridItem.KDR = Math.Round(gridItem.Kills / gridItem.Deaths, 2) >= 0 ? Math.Round(gridItem.Kills / gridItem.Deaths, 2) : 0;
                            gridItem.HSR = Math.Round(gridItem.Headshots / gridItem.Kills, 2) >= 0 ? Math.Round(gridItem.Headshots / gridItem.Kills, 2) : 0;

                            info.ATeamGrid.Add(gridItem);
                        }
                    }

                    query = new SqlCommand($"SELECT (CASE WHEN LOGIN IS NULL THEN p.NAME ELSE LOGIN END), mr.[STEAMID], mr.[KILLS], mr.[DEATHS], mr.[HEADSHOTS], p.RANKNAME, " +
                        $"[OPENFRAGS], [TRIPPLES], [QUADROS], [RAMPAGES], p.VKID, p.ID, p.MMR, p.PHOTO FROM [dbo].[MatchesResults] as mr WITH (NOLOCK) INNER JOIN [dbo].[Players] as p WITH (NOLOCK) ON p.STEAMID = mr.STEAMID " +
                        $"INNER JOIN MatchesHighlights as mh WITH (NOLOCK) ON mh.STEAMID = mr.STEAMID WHERE mh.ID = {id} and mr.ID = {id} and mr.TEAMNAME = '{info.BName}'", Db);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            info.BMPlayers.Add(new MatchInfo.MPlayerInfo()
                            {
                                Name = reader[0].ToString(),
                                DisplayName = reader[0].ToString(),
                                SteamID = reader[1].ToString(),
                                Url = "/images/ranks/" + reader[5].ToString() + ".png",
                                VkID = reader[10].ToString(),
                                ID = reader[11].ToString(),
                                AvatarUrl = reader[13].ToString().Length > 0 ? reader[13].ToString() : "/images/logo_ktv.png"
                            });

                            info.BTeamMMR.Add(double.Parse(reader[12].ToString()));

                            var gridItem = new MatchInfo.MatchStatsGrid()
                            {
                                Name = reader[0].ToString(),
                                SteamID = reader[1].ToString(),
                                Kills = double.Parse(reader[2].ToString()),
                                Deaths = double.Parse(reader[3].ToString()),
                                Headshots = double.Parse(reader[4].ToString()),
                                MMR = int.Parse(reader["MMR"].ToString()),
                                PictureUrl = "/images/ranks/" + reader[5].ToString() + ".png",
                                OpenFrags = int.Parse(reader[6].ToString()),
                                Triples = int.Parse(reader[7].ToString()),
                                Quadros = int.Parse(reader[8].ToString()),
                                Aces = int.Parse(reader[9].ToString()),
                                VkID = reader[10].ToString(),
                                ID = reader[11].ToString(),
                                AvatarUrl = reader[13].ToString().Length > 0 ? reader[13].ToString() : "/images/logo_ktv.png"
                            };

                            gridItem.KRR = Math.Round(gridItem.Kills / (info.AScore + info.BScore), 2) >= 0 ? Math.Round(gridItem.Kills / (info.AScore + info.BScore), 2) : 0;
                            gridItem.KDR = Math.Round(gridItem.Kills / gridItem.Deaths, 2) >= 0 ? Math.Round(gridItem.Kills / gridItem.Deaths, 2) : 0;
                            gridItem.HSR = Math.Round(gridItem.Headshots / gridItem.Kills, 2) >= 0 ? Math.Round(gridItem.Headshots / gridItem.Kills, 2) : 0;

                            info.BTeamGrid.Add(gridItem);
                        }
                    }

                    query = new SqlCommand($"SELECT DEMONAME FROM [dbo].[MatchesDemos] WHERE ID = {id}", Db);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            info.DemoUrl = reader[0].ToString();
                        }
                    }

                    query = new SqlCommand($"SELECT MVP FROM [dbo].[MatchesMVP] WHERE ID = {id}", Db);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            info.MVP = reader[0].ToString();
                        }
                    }

                    query = new SqlCommand($"SELECT MATCHDATE FROM [dbo].[Matches] WITH (NOLOCK) WHERE ID = {id}", Db);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            info.MatchDate = DateTime.Parse(reader[0].ToString()).ToString("dd.MM.yyyy HH:mm");
                        }
                    }

                    DateTime from = new();
                    DateTime to = new();

                    query = new SqlCommand($"SELECT TOP(1) DATETIME FROM [dbo].[MatchesLogs] WITH (NOLOCK) WHERE MATCHID = {id} ORDER BY DATETIME ASC", Db);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            from = DateTime.Parse(reader[0].ToString());
                        }
                    }

                    query = new SqlCommand($"SELECT TOP(1) DATETIME FROM [dbo].[MatchesLogs] WITH (NOLOCK) WHERE MATCHID = {id} ORDER BY DATETIME DESC", Db);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            to = DateTime.Parse(reader[0].ToString());
                        }
                    }

                    info.MatchLength = to.Subtract(from).Duration().ToString(@"hh\:mm\:ss");

                    query = new SqlCommand($"SELECT MESSAGE FROM [dbo].[MatchesLogs] WITH (NOLOCK) WHERE MATCHID = {id} ORDER BY DATETIME DESC", Db);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            info.MatchLog.Add(new MatchInfo.Log() { Record = reader[0].ToString() });
                        }
                    }

                    #endregion
                }
                catch (Exception)
                {
                    //
                }
            }
            else
            {
                info.IsFinished = false;
                List<TotalMatchEx2> _matches = [];
                List<TotalMatchEx2> matches = [];

                using (query = new SqlCommand($"GetLiveMatcheByID", Db))
                {
                    query.CommandType = System.Data.CommandType.StoredProcedure;
                    query.Parameters.AddWithValue("@ID", id);

                    using SqlDataReader reader = query.ExecuteReader();
                    while (reader.Read())
                    {
                        TotalMatchEx2 match = new()
                        {
                            Id = int.Parse(reader[0].ToString()),
                            MapName = reader[4].ToString(),
                            MatchDate = "LIVE",
                            ServerId = int.Parse(reader[3].ToString()),
                            Kills = double.Parse(reader[7].ToString()),
                            Deaths = double.Parse(reader[8].ToString()),
                            Headshots = double.Parse(reader[9].ToString()),
                            VkID = reader[16].ToString(),
                            ID = reader[17].ToString(),
                            MMR = double.Parse(reader[18].ToString()),
                            PhotoUrl = reader[19]?.ToString().Length > 0 ? reader[19].ToString() : "/images/logo_ktv.png"
                        };
                        string aScore = reader[1].ToString();
                        string bScore = reader[2].ToString();
                        if (aScore.Length == 1)
                        {
                            aScore = "0" + aScore;
                        }
                        if (bScore.Length == 1)
                        {
                            bScore = "0" + bScore;
                        }
                        match.Score = aScore + " - " + bScore;
                        match.Name = reader[5].ToString();
                        match.SteamID = reader[6].ToString();
                        match.Team = reader[10].ToString();
                        match.Rank = reader[11].ToString();
                        match.OpenFrags = int.TryParse(reader[12].ToString(), out int res) ? res : 0;
                        match.Tripples = int.TryParse(reader[13].ToString(), out res) ? res : 0;
                        match.Quadros = int.TryParse(reader[14].ToString(), out res) ? res : 0;
                        match.Rampages = int.TryParse(reader[15].ToString(), out res) ? res : 0;
                        _matches.Add(match);
                    }
                }

                var distinct = _matches.DistinctBy(x => x.Id);
                foreach (var match in distinct)
                {
                    TotalMatchEx2 matchEx = new()
                    {
                        Id = match.Id,
                        ServerId = match.ServerId,
                        Score = match.Score,
                        MatchDate = match.MatchDate,
                        MapName = match.MapName,
                    };

                    var tags = (from p in _matches.Where(x => x.Id == match.Id) select new Models.Matches.Player { Name = p.Name, Team = p.Team }).ToList();
                    var names = GetTeamNames(tags);
                    matchEx.ATeam = names["TERRORIST"];
                    matchEx.BTeam = names["CT"];

                    matches.Add(matchEx);
                }

                var m = matches.FirstOrDefault();
                info.AName = m.ATeam.Length > 20 ? m.ATeam[..20] : m.ATeam;
                info.BName = m.BTeam.Length > 20 ? m.BTeam[..20] : m.BTeam;
                info.AScore = double.Parse(m.Score.Split('-', StringSplitOptions.RemoveEmptyEntries).ToList().FirstOrDefault());
                info.BScore = double.Parse(m.Score.Split('-', StringSplitOptions.RemoveEmptyEntries).ToList().LastOrDefault());
                info.MapName = m.MapName;
                info.ServerName = "kTVCSS #" + m.ServerId;

                foreach (var i in _matches.Where(x => x.Team == "TERRORIST"))
                {
                    info.AMPlayers.Add(new MatchInfo.MPlayerInfo()
                    {
                        Name = i.Name,
                        DisplayName = i.Name,
                        SteamID = i.SteamID,
                        Url = "/images/ranks/" + i.Rank + ".png",
                        VkID = i.VkID,
                        ID = i.ID,
                        AvatarUrl = i.PhotoUrl
                    });

                    info.ATeamMMR.Add(i.MMR);

                    var gridItem = new MatchInfo.MatchStatsGrid()
                    {
                        Name = i.Name,
                        SteamID = i.SteamID,
                        Kills = i.Kills,
                        Deaths = i.Deaths,
                        Headshots = i.Headshots,
                        MMR = (int)i.MMR,
                        PictureUrl = "/images/ranks/" + i.Rank + ".png",
                        OpenFrags = i.OpenFrags,
                        Triples = i.Tripples,
                        Quadros = i.Quadros,
                        Aces = i.Rampages,
                        VkID = i.VkID,
                        ID = i.ID,
                        AvatarUrl = i.PhotoUrl
                    };

                    gridItem.KRR = Math.Round(gridItem.Kills / (info.AScore + info.BScore), 2) >= 0 ? Math.Round(gridItem.Kills / (info.AScore + info.BScore), 2) : 0;
                    gridItem.KDR = Math.Round(gridItem.Kills / gridItem.Deaths, 2) >= 0 ? Math.Round(gridItem.Kills / gridItem.Deaths, 2) : 0;
                    gridItem.HSR = Math.Round(gridItem.Headshots / gridItem.Kills, 2) >= 0 ? Math.Round(gridItem.Headshots / gridItem.Kills, 2) : 0;

                    info.ATeamGrid.Add(gridItem);
                }

                foreach (var i in _matches.Where(x => x.Team == "CT"))
                {
                    info.BMPlayers.Add(new MatchInfo.MPlayerInfo()
                    {
                        Name = i.Name,
                        DisplayName = i.Name,
                        SteamID = i.SteamID,
                        Url = "/images/ranks/" + i.Rank + ".png",
                        VkID = i.VkID,
                        ID = i.ID,
                        AvatarUrl = i.PhotoUrl
                    });

                    info.BTeamMMR.Add(i.MMR);

                    var gridItem = new MatchInfo.MatchStatsGrid()
                    {
                        Name = i.Name,
                        SteamID = i.SteamID,
                        Kills = i.Kills,
                        Deaths = i.Deaths,
                        Headshots = i.Headshots,
                        MMR = (int)i.MMR,
                        PictureUrl = "/images/ranks/" + i.Rank + ".png",
                        OpenFrags = i.OpenFrags,
                        Triples = i.Tripples,
                        Quadros = i.Quadros,
                        Aces = i.Rampages,
                        VkID = i.VkID,
                        ID = i.ID,
                        AvatarUrl = i.PhotoUrl
                    };

                    gridItem.KRR = Math.Round(gridItem.Kills / (info.AScore + info.BScore), 2) >= 0 ? Math.Round(gridItem.Kills / (info.AScore + info.BScore), 2) : 0;
                    gridItem.KDR = Math.Round(gridItem.Kills / gridItem.Deaths, 2) >= 0 ? Math.Round(gridItem.Kills / gridItem.Deaths, 2) : 0;
                    gridItem.HSR = Math.Round(gridItem.Headshots / gridItem.Kills, 2) >= 0 ? Math.Round(gridItem.Headshots / gridItem.Kills, 2) : 0;

                    info.BTeamGrid.Add(gridItem);
                }

                DateTime from = new();
                DateTime to = new();

                query = new SqlCommand($"SELECT TOP(1) DATETIME FROM [dbo].[MatchesLogs] WITH (NOLOCK) WHERE MATCHID = {id} ORDER BY DATETIME ASC", Db);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        from = DateTime.Parse(reader[0].ToString());
                    }
                }

                query = new SqlCommand($"SELECT TOP(1) DATETIME FROM [dbo].[MatchesLogs] WITH (NOLOCK) WHERE MATCHID = {id} ORDER BY DATETIME DESC", Db);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        to = DateTime.Parse(reader[0].ToString());
                    }
                }

                info.MatchDate = from.ToString("dd.MM.yyyy HH:mm");
                info.MatchLength = to.Subtract(from).Duration().ToString(@"hh\:mm\:ss");

                query = new SqlCommand($"SELECT MESSAGE FROM [dbo].[MatchesLogs] WITH (NOLOCK) WHERE MATCHID = {id} ORDER BY DATETIME DESC", Db);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        info.MatchLog.Add(new MatchInfo.Log() { Record = reader[0].ToString() });
                    }
                }
            }

            using (query = new SqlCommand($"SELECT ID, URL FROM Teams AS T WITH (NOLOCK) LEFT JOIN TeamsAvatars AS A WITH (NOLOCK) ON T.ID = A.TEAMID WHERE NAME = '{info.AName}'", Db))
            {
                using SqlDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    info.AID = reader[0].ToString();
                    info.AAvatarUrl = reader[1].ToString().Length > 0 ? reader[1].ToString() : "/images/logo_ktv.png";
                }
            }

            using (query = new SqlCommand($"SELECT ID, URL FROM Teams AS T WITH (NOLOCK) LEFT JOIN TeamsAvatars AS A WITH (NOLOCK) ON T.ID = A.TEAMID WHERE NAME = '{info.BName}'", Db))
            {
                using SqlDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    info.BID = reader[0].ToString();
                    info.BAvatarUrl = reader[1].ToString().Length > 0 ? reader[1].ToString() : "/images/logo_ktv.png";
                }
            }

            foreach (var player in info.ATeamGrid)
            {
                using (query = new SqlCommand($"SELECT MMR, ROUND(KDR, 2), ROUND(AVG, 2) FROM Players WITH (NOLOCK) WHERE ID = {player.ID}", Db))
                {
                    using SqlDataReader reader = query.ExecuteReader();
                    while (reader.Read())
                    {
                        info.APopovers.Add(new MatchInfo.PopoverItem()
                        {
                            Name = player.Name,
                            MMR = reader[0].ToString(),
                            KDR = reader[1].ToString(),
                            AVG = reader[2].ToString()
                        });
                    }
                }
            }

            foreach (var player in info.BTeamGrid)
            {
                using (query = new SqlCommand($"SELECT MMR, ROUND(KDR, 2), ROUND(AVG, 2) FROM Players WITH (NOLOCK) WHERE ID = {player.ID}", Db))
                {
                    using SqlDataReader reader = query.ExecuteReader();
                    while (reader.Read())
                    {
                        info.BPopovers.Add(new MatchInfo.PopoverItem()
                        {
                            Name = player.Name,
                            MMR = reader[0].ToString(),
                            KDR = reader[1].ToString(),
                            AVG = reader[2].ToString()
                        });
                    }
                }
            }

            info.ATeamMMR.RemoveAll(x => x == 0);
            info.BTeamMMR.RemoveAll(x => x == 0);
            info.ATeamAVG = Math.Round(info.ATeamMMR.Sum() / info.ATeamMMR.Count);
            info.BTeamAVG = Math.Round(info.BTeamMMR.Sum() / info.BTeamMMR.Count);
            if (info.ATeamAVG.ToString().Contains("число"))
            {
                info.ATeamAVG = 0;
            }
            if (info.BTeamAVG.ToString().Contains("число"))
            {
                info.BTeamAVG = 0;
            }

            foreach (var item in info.ATeamGrid)
            {
                if (info.MVP == item.SteamID)
                {
                    info.ATeamGrid.Where(x => x.SteamID == info.MVP).FirstOrDefault().Name = "★ " + item.Name;
                }
            }

            foreach (var item in info.BTeamGrid)
            {
                if (info.MVP == item.SteamID)
                {
                    info.BTeamGrid.Where(x => x.SteamID == info.MVP).FirstOrDefault().Name = "★ " + item.Name;
                }
            }

            info.AName = info.AName.Length > 20 ? info.AName[..20] : info.AName;
            info.BName = info.BName.Length > 20 ? info.BName[..20] : info.BName;

            var aBest = info.ATeamGrid.OrderByDescending(x => x.MMR).FirstOrDefault().ID;
            var bBest = info.BTeamGrid.OrderByDescending(x => x.MMR).FirstOrDefault().ID;

            EnsureConnected();

            info.ABestPlayer = Db.QueryFirstOrDefault<BestTeamPlayer>($"SELECT ID, (CASE WHEN LOGIN IS NULL THEN NAME ELSE LOGIN END) AS Name, PHOTO as PhotoURL, ROUND(MMR, 2) AS MMR, ROUND(AVG, 2) AS AVG, ROUND(KDR, 2) AS KDR, ROUND(HSR, 2) AS HSR, Round(WINRATE, 2) as Winrate FROM Players WHERE ID = {aBest}");
            info.BBestPlayer = Db.QueryFirstOrDefault<BestTeamPlayer>($"SELECT ID, (CASE WHEN LOGIN IS NULL THEN NAME ELSE LOGIN END) AS Name, PHOTO as PhotoURL, ROUND(MMR, 2) AS MMR, ROUND(AVG, 2) AS AVG, ROUND(KDR, 2) AS KDR, ROUND(HSR, 2) AS HSR, Round(WINRATE, 2) as Winrate FROM Players WHERE ID = {bBest}");

            if (info.ABestPlayer is not null)
            {
                if (info.ABestPlayer.PhotoUrl is null)
                {
                    info.ABestPlayer.PhotoUrl = "/images/logo_ktv.png";
                }
            }
            if (info.BBestPlayer is not null)
            {
                if (info.BBestPlayer.PhotoUrl is null)
                {
                    info.BBestPlayer.PhotoUrl = "/images/logo_ktv.png";
                }
            }

            info.ATeamGrid = [.. info.ATeamGrid.OrderByDescending(x => x.Kills)];
            info.BTeamGrid = [.. info.BTeamGrid.OrderByDescending(x => x.Kills)];

            foreach (var player in info.ATeamGrid)
            {
                player.Damage = Db.QueryFirstOrDefault<double>($"SELECT DAMAGE FROM MatchesDamage WITH (NOLOCK) WHERE MATCHID = {id} AND STEAMID = '{player.SteamID}'");
                player.ADR = Math.Round(player.Damage / (info.AScore + info.BScore));
            }

            foreach (var player in info.BTeamGrid)
            {
                player.Damage = Db.QueryFirstOrDefault<double>($"SELECT DAMAGE FROM MatchesDamage WITH (NOLOCK) WHERE MATCHID = {id} AND STEAMID = '{player.SteamID}'");
                player.ADR = Math.Round(player.Damage / (info.AScore + info.BScore));
            }

            return info;
        }

        public string GetSourceTV(int id)
        {
            EnsureConnected();

            return "ktvcss.ru:" + Db.QueryFirst<string>($"SELECT SourceTV FROM MatchesLive INNER JOIN GameServers ON MatchesLive.SERVERID = GameServers.ID WHERE MatchesLive.ID = {id}");
        }
    }
}
