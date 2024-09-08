using Dapper;
using kTVCSSBlazor.Components.Pages.Teams;
using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Models.Teams;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using static MudBlazor.Colors;

namespace kTVCSSBlazor.Db.Repository
{
    public class Teams : Context, ITeams
    {
        public Teams(IConfiguration configuration, ILogger logger) : base(configuration, logger)
        {
            TeamsAutoDeletion();
        }

        public List<Team> Get()
        {
            EnsureConnected();

            return Db.Query<Team>("GetTeamsList", commandType: CommandType.StoredProcedure).ToList();
        }

        public TeamPageData GetTeamByID(int id)
        {
            EnsureConnected();

            TeamPageData data = new();

            SqlCommand query = new($"SELECT [NAME],[CAPTAINSTEAMID],[CAPTAINVKID],[MATCHESPLAYED],[MATCHESWINS]," +
                    $"[MATCHESLOOSES],[RATING],[WINRATE],[ISBLOCKEDITING],[TIER], DESCRIPTION FROM [kTVCSS].[dbo].[Teams] WHERE ID = {id}", Db);
            using (SqlDataReader reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    data.Info.Name = reader[0].ToString();
                    data.Info.Description = reader[10].ToString();
                    data.Info.CapSteamID = reader[1].ToString();
                    data.Info.CapVKID = reader[2].ToString();
                    data.Info.MatchesPlayed = int.Parse(reader[3].ToString());
                    data.Info.MatchesWins = int.Parse(reader[4].ToString());
                    data.Info.MatchesLosts = int.Parse(reader[5].ToString());
                    data.Info.Rating = int.Parse(reader[6].ToString());
                    data.Info.WinRate = reader[7].ToString();
                    data.Info.BlockEdit = int.Parse(reader[8].ToString());
                    data.Info.Tier = int.Parse(reader[9].ToString());
                }
            }

            query = new SqlCommand($"SELECT STEAMID FROM [kTVCSS].[dbo].[TeamsMembers] WHERE TEAMID = {id}", Db);
            using (SqlDataReader reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    data.Members.Add(new Player() { SteamID = reader[0].ToString() });
                }
            }

            foreach (var player in data.Members)
            {
                using (query = new SqlCommand($"SELECT * FROM Players WITH (NOLOCK) WHERE STEAMID = '{player.SteamID}'", Db))
                {
                    using SqlDataReader reader = query.ExecuteReader();
                    while (reader.Read())
                    {
                        double kdr = 0; string hsr = "0"; double avg = 0; string winrate = "0"; DateTime ld = new(0);
                        try
                        {
                            kdr = Math.Round(double.Parse(reader["KDR"].ToString()), 2);
                            hsr = (Math.Round(Math.Round(double.Parse(reader["HSR"].ToString()), 2) * 100, 2)).ToString() + "%";
                            avg = Math.Round(double.Parse(reader["AVG"].ToString()), 2);
                            winrate = Math.Round(double.Parse(reader["WINRATE"].ToString())) + "%";
                            ld = DateTime.Parse(reader["LASTMATCH"]?.ToString());
                        }
                        catch (Exception)
                        {
                            //
                        }

                        var member = data.Members.FirstOrDefault(x => x.SteamID == player.SteamID);

                        if (member != null)
                        {
                            // Обновить поля объекта
                            member.Id = int.Parse(reader[0].ToString());
                            member.Name = string.IsNullOrEmpty(reader["LOGIN"].ToString()) ? reader[1].ToString() : reader["LOGIN"].ToString();
                            member.SteamID = reader[2].ToString();
                            member.Kills = int.Parse(reader[3].ToString());
                            member.Deaths = int.Parse(reader[4].ToString());
                            member.Headshots = int.Parse(reader[5].ToString());
                            member.KDR = kdr;
                            member.HSR = hsr;
                            member.MMR = int.Parse(reader["MMR"].ToString());
                            member.AVG = avg;
                            member.RankName = "/images/ranks/" + reader["RANKNAME"].ToString() + ".png";
                            member.MatchesTotal = int.Parse(reader["MATCHESPLAYED"].ToString());
                            member.Wons = int.Parse(reader["MATCHESWINS"].ToString());
                            member.Losts = int.Parse(reader["MATCHESLOOSES"].ToString());
                            member.PhotoUrl = reader["PHOTO"].ToString();
                            member.Calibration = int.Parse(reader["ISCALIBRATION"].ToString());
                            member.LastMatch = ld > new DateTime(2000, 1, 1) ? ld.ToString("dd.MM.yyyy HH:mm") : "еще не играл";
                            member.Winrate = winrate;
                            member.Block = int.TryParse(reader["BLOCK"]?.ToString() ?? "0", out int blk) ? blk : 0;
                        }
                    }
                }

                player.EntryDate = Db.QueryFirstOrDefault<DateTime>($"SELECT TOP(1) DATETIME FROM TeamsMembersLastJoins WHERE STEAMID = '{player.SteamID}' ORDER BY DATETIME DESC").ToString("dd.MM.yyyy") ?? "неизвестно";
            }

            data.AvatarUrl = Db.QueryFirstOrDefault<string>($"SELECT URL FROM TeamsAvatars WHERE TEAMID = {id}") ?? "/images/logo_ktv.png";

            var allTeams = Db.Query<string>("SELECT NAME FROM Teams WITH (NOLOCK) ORDER BY RATING DESC");

            data.Info.Position = allTeams.ToList().IndexOf(data.Info.Name) + 1;

            data.CreationDate = Db.QueryFirstOrDefault<DateTime>($"SELECT TOP(1) DATETIME FROM TeamsLastCreations WHERE STEAMID = '{data.Info.CapSteamID}' ORDER BY DATETIME DESC").ToString("dd.MM.yyyy") ?? "неизвестно";

            data.Achiviments = Db.Query<Achiviment>($"SELECT Place, Name FROM TeamsAchievements WHERE TEAMID = {id}").ToList();

            try
            {
                query = new SqlCommand($"[dbo].[GetLastTwentyMatchesOfTeam]", Db);
                query.CommandType = CommandType.StoredProcedure;
                query.Parameters.AddWithValue("@TNAME", data.Info.Name);
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
                        data.Matches.Add(new TeamMatch()
                        {
                            Result = $"{reader[1]} {reader[3]} - {reader[4]} {reader[2]}",
                            ID = int.Parse(reader[0].ToString()),
                            MapName = reader[6].ToString(),
                            DateTime = DateTime.Parse(reader[5].ToString()),
                            Victory = winner.ToLower() == data.Info.Name.ToLower()
                        });
                    }
                }
            }
            catch (Exception) { }

            return data;
        }

        private T ExecuteStoredProcedureWithReturnValue<T>(string spName, DynamicParameters dynamicParameters) where T : new()
        {
            dynamicParameters.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            Db.Execute(spName, dynamicParameters, commandType: CommandType.StoredProcedure);
            return dynamicParameters.Get<T>("ReturnValue");
        }

        public int DeleteTeam(int id, string steam)
        {
            EnsureConnected();

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("TEAMID", id);
            dynamicParameters.Add("REQUEST", steam);

            return ExecuteStoredProcedureWithReturnValue<int>("DeleteTeam", dynamicParameters);
        }

        public int LeaveTeam(string steam)
        {
            EnsureConnected();

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("STEAMID", steam);

            dynamicParameters.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            Db.Execute("TeamsRemovePlayer", dynamicParameters, commandType: CommandType.StoredProcedure);
            return dynamicParameters.Get<int>("ReturnValue");
        }

        public int KickFromTeam(int id, string steam)
        {
            EnsureConnected();

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("STEAMID", steam);
            return ExecuteStoredProcedureWithReturnValue<int>("TeamsRemovePlayer", dynamicParameters);

            Logger.LogDebug($"{steam} был исключен из команды #{id}");
        }

        public int MakeInvite(string captain, string invited)
        {
            EnsureConnected();

            DynamicParameters dynamicParameters = new DynamicParameters();

            dynamicParameters.Add("CAPTAINSTEAM", captain);
            dynamicParameters.Add("INVITEDSTEAM", invited);

            return ExecuteStoredProcedureWithReturnValue<int>("MakeInvite", dynamicParameters);
        }

        public string CheckInvite(string steam)
        {
            string team = "";

            EnsureConnected();

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("STEAMID", steam);

            dynamicParameters.Add("ReturnValue", dbType: DbType.String, direction: ParameterDirection.ReturnValue);
            team = Db.QueryFirstOrDefault<string>("CheckInvite", dynamicParameters, commandType: CommandType.StoredProcedure);

            return string.IsNullOrEmpty(team) ? "" : team;
        }

        public int AcceptInvite(string steam)
        {
            EnsureConnected();

            DynamicParameters p = new DynamicParameters();
            p.Add("STEAMID", steam);
            return ExecuteStoredProcedureWithReturnValue<int>("AcceptInvite", p);
        }

        public int KickFromTeam(string steam)
        {
            EnsureConnected();

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("STEAMID", steam);
            return ExecuteStoredProcedureWithReturnValue<int>("TeamsRemovePlayer", dynamicParameters);
        }

        public bool IsPlayerCaptain(string steam)
        {
            EnsureConnected();

            string team = Db.QueryFirstOrDefault<string>($"SELECT NAME FROM Teams WHERE CAPTAINSTEAMID = '{steam}'");

            return !string.IsNullOrEmpty(team);
        }

        public void DeclineInvite(string steam)
        {
            EnsureConnected();

            Db.Execute($"DELETE FROM TeamsInvites WHERE INVITEDSTEAMID = '{steam}'");
        }

        public int GetTeamBySteamID(string steam)
        {
            EnsureConnected();

            int result = Db.QueryFirstOrDefault<int>($"SELECT ID FROM Teams WHERE CAPTAINSTEAMID = '{steam}'");

            if (result == 0 || result == null)
            {
                result = Db.QueryFirstOrDefault<int>($"SELECT TEAMID FROM TeamsMembers WHERE STEAMID = '{steam}'");
            }

            return result > 0 || result != null ? result : 0;
        }

        public int CreateTeam(string name, string steam)
        {
            EnsureConnected();

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("NAME", name);
            dynamicParameters.Add("STEAMID", steam);
            dynamicParameters.Add("VKID", null);

            return ExecuteStoredProcedureWithReturnValue<int>("CreateTeam", dynamicParameters);
        }

        public TeamEdit GetTeamForEdit(int id)
        {
            EnsureConnected();

            return Db.QueryFirst<TeamEdit>($"SELECT NAME, DESCRIPTION, URL, ID FROM [kTVCSS].[dbo].[Teams] LEFT JOIN TeamsAvatars on Teams.ID = TeamsAvatars.TEAMID WHERE ID = {id}");
        }

        public int SaveTeam(TeamEdit team)
        {
            EnsureConnected();

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("NAME", team.Name);
            dynamicParameters.Add("DESCRIPTION", team.Description);
            dynamicParameters.Add("AVATAR", team.Url);
            dynamicParameters.Add("ID", team.Id);

            return ExecuteStoredProcedureWithReturnValue<int>("UpdateTeamProfile", dynamicParameters);
        }

        public void SaveTeamAchiviments(int id, string achvs)
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

                    Db.Execute($"DELETE FROM TeamsAchievements WHERE TEAMID = {id}");

                    foreach (var ach in achiviments)
                    {
                        Db.Execute($"INSERT INTO [TeamsAchievements] VALUES ({id}, '{ach.Name}', {ach.Place})");
                    }
                }
                else
                {
                    Db.Execute($"DELETE FROM TeamsAchievements WHERE TEAMID = {id}");
                }
            }
        }

        public async Task TeamsAutoDeletion()
        {
            EnsureConnected();

            var teams = Db.Query<Team>("GetTeamsListForce", commandType: CommandType.StoredProcedure).ToList();

            foreach (var team in teams)
            {
                if (team.PlayersCount < 4)
                {
                    Db.ExecuteAsync($"DELETE FROM Teams WHERE ID = {team.Id}");
                    Db.ExecuteAsync($"DELETE FROM TeamsMembers WHERE TEAMID = {team.Id}");
                }
            }
        }
    }
}
