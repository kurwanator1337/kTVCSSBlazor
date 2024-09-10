using Dapper;
using kTVCSSBlazor.Data;
using kTVCSSBlazor.Db;
using kTVCSSBlazor.Hubs.Models;
using kTVCSSBlazor.Models;
using Microsoft.AspNetCore.SignalR;
using Renci.SshNet.Messages;
using Renci.SshNet;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using static Dapper.SqlMapper;
using static MudBlazor.CategoryTypes;
using Telegram.Bot;
using Newtonsoft.Json.Linq;
using kTVCSSBlazor.Db.Interfaces;

namespace kTVCSSBlazor.Hubs
{
    public class kTVCSSHub : Hub
    {
        public static ConcurrentDictionary<string, User> OnlineUsers = new ConcurrentDictionary<string, User>();
        public static ConcurrentDictionary<string, User> SearchUsers = new ConcurrentDictionary<string, User>();
        public static List<Mix> Mixes = new List<Mix>();
        public static ConcurrentDictionary<string, int> OnlinePageUsers = new();
        public static ConcurrentDictionary<string, int> FriendsPageUsers = new();
        public SqlConnection Db { get; set; }
        public static kTVCSSHub Instance { get; set; }
        private string _connectionString { get; set; }
        private ILogger logger { get; set; }
        private IVips vips { get; set; }
        private IConfiguration configuration { get; set; }
        public static bool Checking = false;
        private string alertToken = "";
        private Telegram.Bot.TelegramBotClient botClient { get; set; }

        private const int ReadyCount = 10;

        #region диалоги

        public async Task SendDM(string fromName, int from, int to, string msg)
        {
            var player = OnlineUsers.Where(x => x.Value.Id == to);

            if (player is not null)
            {
                try
                {
                    Clients.Client(player.First().Key).SendAsync("GetPrivateMessage", $"{fromName}: {msg}", from);
                }
                catch (Exception)
                {
                    // null?
                    EnsureConnected();

                    DynamicParameters d = new DynamicParameters();
                    d.Add("ID", to);
                    d.Add("TEXT", $"У Вас новое сообщение от {fromName}");

                    Db.Execute($"AddAlert", d, commandType: CommandType.StoredProcedure);
                }
            }
            else
            {
                EnsureConnected();

                DynamicParameters d = new DynamicParameters();
                d.Add("ID", to);
                d.Add("TEXT", $"У Вас новое сообщение от {fromName}");

                Db.Execute($"AddAlert", d, commandType: CommandType.StoredProcedure);
            }
        }

        #endregion

        #region сообщения

        public static Queue<string> Messages = new Queue<string>();

        public async Task SendMessage(User user, string message)
        {
            Messages.Enqueue($"[{DateTime.Now.ToShortTimeString()}] {user.Username}: {message}");
            Clients.All.SendAsync("GetMessage", $"[{DateTime.Now.ToShortTimeString()}] {user.Username}: {message}");
        }

        public async Task SendPrivateMessage(User from, int to, string message)
        {
            try
            {
                var player = OnlineUsers.Where(x => x.Value.Id == to);

                if (player is not null)
                {
                    Clients.Client(player.First().Key)
                        .SendAsync("GetPrivateMessage", $"{from.Username}: {message}", from.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        public async Task LoadMessages()
        {
            var a = Messages.TakeLast(10).ToList();

            if (a.Count == 0)
            {
                Clients.Caller.SendAsync("GetMessage", "Добро пожаловать в чат!");
            }

            foreach (var o in a)
            {
                Clients.Caller.SendAsync("GetMessage", o);
            }
        }

        #endregion

        #region Координатор

        public kTVCSSHub(string connectionString, ILogger logger, IConfiguration cfg, IVips vips)
        {
            _connectionString = connectionString;
            this.vips = vips;
            Db = new SqlConnection(connectionString);
            PlayersDirector();
            Instance = this;
            this.logger = logger;
            configuration = cfg;
            alertToken = configuration.GetValue<string>("tgMatchAlertBotToken");
            botClient = new Telegram.Bot.TelegramBotClient(alertToken);
        }

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
                Db = new SqlConnection(_connectionString);
                EnsureConnected();
            }
        }

        public async Task PlayersDirector()
        {
            while (true)
            {
                Checking = true;

                EnsureConnected();

                await FixOnlineNSearchUsers();

                //               mmr:min   max
                await PlayersChooser(2001, 5000);
                await PlayersChooser(1701, 2000);
                await PlayersChooser(1251, 1701);
                await PlayersChooser(1251, 2000);
                await PlayersChooser(1701, 5000);
                await PlayersChooser(0, 500);
                await PlayersChooser(0, 1000);
                await PlayersChooser(0, 1700);
                await PlayersChooser(0, 5000);
                await PlayersChooser(0, 0);

                Checking = false;

                await Task.Delay(60000);
            }
        }

        private async Task FixOnlineNSearchUsers()
        {
            var tmp = new ConcurrentDictionary<string, User>();

            foreach (var user in SearchUsers)
            {
                if (!OnlineUsers.Where(x => x.Key == user.Key).Any())
                {
                    tmp.TryAdd(user.Key, user.Value);
                }
            }

            foreach (var user in tmp)
            {
                SearchUsers.TryRemove(user.Key, out User tm);
            }
        }

        private bool IsPlayerBanned(string steam)
        {
            EnsureConnected();

            int result = 0;

            try
            {
                result = Db.QueryFirstOrDefault<int>($"SELECT BLOCK FROM Players WHERE STEAMID = '{steam}'");
            }
            catch (Exception)
            {
                return false;
            }

            return result == 1;
        }

        private bool IsPlayerBanned(int id)
        {
            EnsureConnected();

            int result = 0;

            try
            {
                result = Db.QueryFirst<int>($"SELECT BLOCK FROM Players WHERE ID = '{id}'");
            }
            catch (Exception)
            {
                result = 0;
            }

            return result == 1;
        }

        private void SetServerBusy(int id)
        {
            EnsureConnected();

            Db.Execute($"UPDATE GameServers SET BUSY = 1 WHERE ID = {id}");

            logger.LogDebug($"Сервер {id} был занят миксом");
        }

        private int GetFreeServer(out int id)
        {
            id = 0;

            EnsureConnected();

            using (SqlCommand query =
                   new SqlCommand($"SELECT ID FROM GameServers WHERE BUSY = 0 AND TYPE = 1 AND ENABLED = 1", Db))
            {
                int.TryParse(query.ExecuteScalar()?.ToString() ?? "0", out id);
            }

            return id;
        }

        private void BlockPlayerBySteam(string steam, string reason, string author)
        {
            EnsureConnected();

            DynamicParameters p = new DynamicParameters();
            p.Add("STEAMID", steam);
            p.Add("REASON", reason);
            p.Add("BY", author);

            Db.Execute("BanPlayerBySteam", p, commandType: CommandType.StoredProcedure);

            logger.LogInformation($"{author} выдал бан игроку {steam} с причиной {reason}");
        }

        private async Task CreateMatch(List<AwaitingPlayer> players)
        {
            EnsureConnected();

            GetFreeServer(out int freeServer);

            if (freeServer == 0)
            {
                foreach (var player in players)
                {
                    Clients.Client(player.ConnectionID).SendAsync("NoFreeServers");
                }

                return;
            }

            foreach (var user in players)
            {
                players.Where(x => x.Id == user.Id).FirstOrDefault().Ready = true;
            }

            EnsureConnected();

            Mix mix = new Mix();

            var lastMaps = Db.Query<string>("SELECT TOP(6) MAP FROM Matches ORDER BY ID DESC");

            string map = Db.QueryFirst<string>("SELECT TOP 1 MAP FROM [kTVCSS].[dbo].[MixesMaps] ORDER BY NEWID()");

            while (lastMaps.Contains(map))
            {
                map = Db.QueryFirst<string>("SELECT TOP 1 MAP FROM [kTVCSS].[dbo].[MixesMaps] ORDER BY NEWID()");
            }

            mix.MapName = map;

            mix.MapImage = $"/images/mapsbackgrs/{mix.MapName}.jpg";
            mix.ServerID = freeServer;

            var address = Db.QueryFirst<string>($"SELECT PUBLICADDRESS FROM GameServers WHERE ID = {freeServer}");
            var port = Db.QueryFirst<string>($"SELECT GAMEPORT FROM GameServers WHERE ID = {freeServer}");

            mix.ServerAddress = $"{address}:{port}";

            List<AwaitingPlayer> mixPlayers = new List<AwaitingPlayer>();

            List<AwaitingPlayer> team1 = new List<AwaitingPlayer>();
            List<AwaitingPlayer> team2 = new List<AwaitingPlayer>();

            if (players.DistinctBy(x => x.InLobbyWithPlayerID).Count() == 1)
            {
                players = Shuffle(players);

                team1.AddRange(players.Where(x => x.TeamID == "0"));
                team2.AddRange(players.Where(x => x.TeamID == "1"));
            }
            else
            {
                var groupedPlayers = players.GroupBy(p => p.InLobbyWithPlayerID).ToList();

                foreach (var group in groupedPlayers)
                {
                    if (group.Key == 0) continue;

                    if (team1.Count + group.Count() <= 5)
                    {
                        team1.AddRange(group);
                    }
                    else if (team2.Count + group.Count() <= 5)
                    {
                        team2.AddRange(group);
                    }
                }

                if (groupedPlayers.Any())
                {
                    var noTeamPlayers = groupedPlayers.Where(x => x.Key == 0).FirstOrDefault();

                    if (noTeamPlayers is not null)
                    {
                        var ntp = noTeamPlayers.ToList();

                        ntp = Shuffle(ntp);

                        foreach (var player in ntp)
                        {
                            if (team1.Count > team2.Count)
                            {
                                team2.Add(player);
                            }
                            else
                            {
                                team1.Add(player);
                            }
                        }
                    }
                }
            }

            // добавляем записи в бд

            EnsureConnected();

            try
            {
                Db.Execute(
                    $"INSERT INTO Mixes VALUES ('{mix.Guid}', 0, {mix.ServerID}, '{mix.MapName}', '{DateTime.Now.AddMinutes(5).ToString("yyyy-MM-dd HH:mm:ss")}')");

                foreach (var player in team1)
                {
                    player.TeamID = "0";
                    using (SqlCommand query = new SqlCommand($"InsertMixMember", Db))
                    {
                        query.CommandType = CommandType.StoredProcedure;
                        query.Parameters.AddWithValue("@GUID", mix.Guid);
                        query.Parameters.AddWithValue("@STEAMID", player.SteamId);
                        query.Parameters.AddWithValue("@TEAM", 0);
                        query.Parameters.AddWithValue("@CAPTAIN", 0);
                        query.ExecuteNonQuery();
                    }

                    Db.Execute(
                        $"INSERT INTO MixesAllowedPlayers VALUES ('{player.SteamId}', {mix.ServerID}, '{mix.Guid}')");
                }

                foreach (var player in team2)
                {
                    player.TeamID = "1";
                    using (SqlCommand query = new SqlCommand($"InsertMixMember", Db))
                    {
                        query.CommandType = CommandType.StoredProcedure;
                        query.Parameters.AddWithValue("@GUID", mix.Guid);
                        query.Parameters.AddWithValue("@STEAMID", player.SteamId);
                        query.Parameters.AddWithValue("@TEAM", 1);
                        query.Parameters.AddWithValue("@CAPTAIN", 0);
                        query.ExecuteNonQuery();
                    }

                    Db.Execute(
                        $"INSERT INTO MixesAllowedPlayers VALUES ('{player.SteamId}', {mix.ServerID}, '{mix.Guid}')");
                }

                mix.MixPlayers.AddRange(team1);
                mix.MixPlayers.AddRange(team2);

                Mixes.Add(mix);

                SetServerBusy(freeServer);

                foreach (var player in mix.MixPlayers)
                {
                    Clients.Client(player.ConnectionID).SendAsync("JoinMixRoom", mix.Guid.ToString());
                }

                foreach (var player in mix.MixPlayers)
                {
                    try
                    {
                        await SendTelegramAlert(player);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        private async Task SendTelegramAlert(AwaitingPlayer player)
        {
            long tgId = Db.QueryFirstOrDefault<long>(
                $"SELECT TELEGRAMID FROM Players WITH (NOLOCK) WHERE STEAMID = '{player.SteamId}'");

            if (tgId > 0)
            {
                try
                {
                    await botClient.SendTextMessageAsync(new Telegram.Bot.Types.ChatId(tgId), "Игра найдена!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                }
            }
        }

        (List<AwaitingPlayer>, List<AwaitingPlayer>) FindOptimalTeams(List<AwaitingPlayer> players)
        {
            int numberOfPlayers = players.Count;
            int halfTeamSize = numberOfPlayers / 2;
            double minDifference = double.MaxValue;
            List<AwaitingPlayer>? bestTeam1 = null;
            List<AwaitingPlayer>? bestTeam2 = null;

            // Все возможные комбинации по 5 игроков
            var combinations = GetCombinations(players, halfTeamSize);

            foreach (var team1 in combinations)
            {
                var team2 = players.Except(team1).ToList();
                double avgMMRTeam1 = team1.Average(player => player.CurrentMMR);
                double avgMMRTeam2 = team2.Average(player => player.CurrentMMR);
                double difference = Math.Abs(avgMMRTeam1 - avgMMRTeam2);

                if (difference < minDifference)
                {
                    minDifference = difference;
                    bestTeam1 = team1;
                    bestTeam2 = team2;
                }
            }

            return (bestTeam1!, bestTeam2!);
        }

        IEnumerable<List<AwaitingPlayer>> GetCombinations(List<AwaitingPlayer> list, int length)
        {
            if (length == 1) return list.Select(x => new List<AwaitingPlayer> { x });
            return GetCombinations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new List<AwaitingPlayer> { t2 }).ToList());
        }

        public List<AwaitingPlayer> Shuffle(List<AwaitingPlayer> input)
        {
            List<AwaitingPlayer> output = new();
            List<AwaitingPlayer> team1 = new();
            List<AwaitingPlayer> team2 = new();

            foreach (var player in input)
            {
                if (player.CurrentMMR == 0)
                {
                    player.CurrentMMR = 100;
                }
            }

            var optimal = FindOptimalTeams(input);
            team1.AddRange(optimal.Item1);
            team2.AddRange(optimal.Item2);

            foreach (var player in team1)
            {
                player.TeamID = "0";
            }

            foreach (var player in team2)
            {
                player.TeamID = "1";
            }

            output.AddRange(team1);
            output.AddRange(team2);

            foreach (var player in output)
            {
                if (player.CurrentMMR == 100)
                {
                    player.CurrentMMR = 0;
                }
            }

            return output;
        }

        private async Task PlayersChooser(int minMMR, int maxMMR)
        {
            try
            {
                if (SearchUsers.Where(x => x.Value.CurrentMMR >= minMMR && x.Value.CurrentMMR <= maxMMR).ToList()
                            .Count >= ReadyCount)
                {
                    var playersWithLobby = SearchUsers
                        .Where(x => x.Value.InLobbyWithPlayerID != 0 && x.Value.CurrentMMR >= minMMR &&
                                    x.Value.CurrentMMR <= maxMMR);
                    var playersWithoutLobby = SearchUsers
                        .Where(x => x.Value.InLobbyWithPlayerID == 0 && x.Value.CurrentMMR >= minMMR &&
                                    x.Value.CurrentMMR <= maxMMR);

                    var tempWithLobby = playersWithLobby.Take(ReadyCount);
                    var remainingCount = ReadyCount - tempWithLobby.Count();

                    playersWithoutLobby = playersWithoutLobby.OrderBy(x => x.Value.StartSearchDateTime);

                    var tempWithoutLobby = playersWithoutLobby.Take(remainingCount);

                    var mixPlayers = new ConcurrentDictionary<string, User>();
                    foreach (var player in tempWithLobby.Concat(tempWithoutLobby))
                    {
                        mixPlayers.TryAdd(player.Key, player.Value);
                    }

                    foreach (var player in mixPlayers)
                    {
                        SearchUsers.TryRemove(player);
                    }

                    List<AwaitingPlayer> list = new List<AwaitingPlayer>();

                    foreach (var player in mixPlayers)
                    {
                        list.Add(new()
                        {
                            AvatarUrl = player.Value.AvatarUrl,
                            ConnectionID = player.Key,
                            CurrentMMR = player.Value.CurrentMMR,
                            Id = player.Value.Id,
                            InLobbyWithPlayerID = player.Value.InLobbyWithPlayerID,
                            MaxMMR = player.Value.MaxMMR,
                            MinMMR = player.Value.MinMMR,
                            Name = player.Value.Name,
                            Password = player.Value.Password,
                            RankPicture = player.Value.RankPicture,
                            Roles = player.Value.Roles,
                            SteamId = player.Value.SteamId,
                            TeamID = player.Value.TeamID,
                            Ready = false,
                            TeamName = player.Value.TeamName,
                            TeamPicture = player.Value.TeamPicture,
                            Tier = player.Value.Tier,
                            TotalMatches = player.Value.TotalMatches,
                            Username = player.Value.Username,
                            VkUid = player.Value.VkUid
                        });

                        //Clients.Client(player.Key).SendAsync("GameFound");

                        //logger.LogDebug($"Игроку {player.Value.Username} было отправлено сообщение для принятия игры ({player.Key})");
                    }

                    CreateMatch(list);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        #endregion

        #region Mixes

        public async Task StopSearch(User user)
        {
            if (!SearchUsers.Where(x => x.Value.Id == user.Id).Any())
            {
                return;
            }

            var players = SearchUsers.Where(x => x.Value.InLobbyWithPlayerID == user.Id).ToList();

            if (players.Count == 0)
            {
                var player = SearchUsers.Where(x => x.Value.Id == user.Id).FirstOrDefault();
                SearchUsers.TryRemove(player);
                Clients.Client(player.Key).SendAsync("OnDisconnectedFromCoordinator");
            }

            foreach (var player in players)
            {
                SearchUsers.TryRemove(player);

                Clients.Client(player.Key).SendAsync("OnDisconnectedFromCoordinator");
            }

            RefreshFriendsUsers();
            RefreshOnlineUsers();

            try
            {
                Clients.All.SendAsync("MixesSearchCurrentCount", SearchUsers.Count);
            }
            catch (Exception ex)
            {
                // ?
            }

            logger.LogDebug($"{user.Username} остановил поиск ({Context.ConnectionId})");
        }

        public async Task RefreshOnlineUsers()
        {
            foreach (var player in OnlinePageUsers)
            {
                try
                {
                    Clients.Client(player.Key).SendAsync("RefreshOnline");
                }
                catch (Exception)
                {
                    //
                }
            }
        }

        public async Task RefreshFriendsUsers()
        {
            foreach (var player in FriendsPageUsers)
            {
                try
                {
                    Clients.Client(player.Key).SendAsync("RefreshFriends");
                }
                catch (Exception)
                {
                    //
                }
            }
        }

        public async Task StartSearch(User user)
        {
            if (IsPlayerBanned(user.SteamId))
            {
                Clients.Caller.SendAsync("GetActionError", "Нельзя начать поиск, т.к. Вы в бане!");

                return;
            }

            int losts = 0;

            EnsureConnected();

            losts = Db.QuerySingleOrDefault<int>("GetLastThreeLosts", new { user.SteamId },
                commandType: CommandType.StoredProcedure);

            if (losts == 3)
            {
                Clients.Caller.SendAsync("SendLostsAlert");
            }

            try
            {
                EnsureConnected();

                int lc = Db.QueryFirstOrDefault<int>(
                    $"SELECT LICENSEREADED FROM Players WITH (NOLOCK) WHERE ID = {user.Id}");

                if (lc == 0)
                {
                    Clients.Caller.SendAsync("SendLC");
                    return;
                }

                var testDb =
                    Db.QueryFirstOrDefault<string>(
                        $"SELECT STEAMID FROM MixesAllowedPlayers WHERE STEAMID = '{user.SteamId}'");

                if (!string.IsNullOrEmpty(testDb))
                {
                    Clients.Caller.SendAsync("GetActionError", "Нельзя начать поиск, т.к. Вы в матче!");

                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }

            try
            {
                EnsureConnected();

                var testDb =
                    Db.QueryFirstOrDefault<int>($"SELECT ParamValue FROM Settings WHERE ParamName = 'MixesEnabled'");

                if (testDb == 0)
                {
                    var reason =
                        Db.QueryFirstOrDefault<string>(
                            $"SELECT ParamDescription FROM Settings WHERE ParamName = 'MixesEnabled'");

                    Clients.Caller.SendAsync("GetActionError", $"Миксы временно отключены по причине \"{reason}\" 🥲");
                    Clients.Caller.SendAsync("MixesDisabled");

                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }

            if (SearchUsers.Where(x => x.Value.Id == user.Id).Any())
            {
                return;
            }

            var players = OnlineUsers.Where(x => x.Value.InLobbyWithPlayerID == user.Id).ToList();

            if (players.Count == 0)
            {
                var player = OnlineUsers.Where(x => x.Value.Id == user.Id).FirstOrDefault();
                player.Value.StartSearchDateTime = DateTime.Now;

                SearchUsers.TryAdd(player.Key, player.Value);
                Clients.Client(player.Key).SendAsync("OnConnectedToCoordinator");

                logger.LogDebug($"{user.Username} вошел в поиск ({player.Key})");
            }

            foreach (var player in players)
            {
                player.Value.StartSearchDateTime = DateTime.Now;

                SearchUsers.TryAdd(player.Key, player.Value);

                Clients.Client(player.Key).SendAsync("OnConnectedToCoordinator");

                logger.LogDebug($"{user.Username} вошел в поиск ({Context.ConnectionId})");
            }

            if (DateTime.Now.Hour >= 19 && DateTime.Now.Hour <= 23)
            {
                var player = OnlineUsers.FirstOrDefault(x => x.Value.Id == user.Id);

                if (player.Value.CurrentMMR >= 1701)
                {
                    Clients.Client(player.Key).SendAsync("JustT1Started");
                }
            }

            try
            {
                Clients.All.SendAsync("MixesSearchCurrentCount", SearchUsers.Count);
            }
            catch (Exception ex)
            {
                // ?
            }

            RefreshFriendsUsers();
            RefreshOnlineUsers();
        }

        private bool IsPlayerInSearch(User user)
        {
            if (SearchUsers.Where(x => x.Value.Id == user.Id).Any())
            {
                return true;
            }

            return false;
        }

        public async Task SendRequestToJoinLobby(int id, User from)
        {
            if (!await vips.IsPremiumVip(from.SteamId))
            {
                Clients.Caller.SendAsync("GetActionError", "Чтобы приглашать игроков в лобби необходима подписка уровня PREMIUM!");
                return;
            }

            EnsureConnected();

            string invitedSteam = Db.QueryFirst<string>($"SELECT STEAMID FROM Players WHERE ID = {id}");

            if (!await vips.IsPremiumVip(invitedSteam))
            {
                Clients.Caller.SendAsync("GetActionError", "У приглашаемого Вами игрока должна быть подписка уровня PREMIUM!");
                return;
            }

            //Clients.Caller.SendAsync("GetActionError", "Лобби временно отключены разработчиком!");

            //return;

            //if (from.CurrentMMR > 1700)
            //{
            //    Clients.Caller.SendAsync("GetActionError", "Лобби не доступно для Вашего рейтинга!");

            //    return;
            //}

            if (IsPlayerBanned(id))
            {
                Clients.Caller.SendAsync("GetActionError", "Нельзя пригласить игрока, т.к. он в бане!");

                return;
            }

            //if (GetTierByID(id) != (from.CurrentMMR >= 1701 ? 1 : 2))
            //{
            //    Clients.Caller.SendAsync("GetActionError", "Нельзя пригласить игрока, Вы находитесь в разных игровых категориях (разный TIER)!");

            //    return;
            //}

            if (IsPlayerInSearch(from))
            {
                Clients.Caller.SendAsync("GetActionError",
                    "Нельзя пригласить игрока, т.к. Вы находитесь в поиске матча!");

                return;
            }

            var player = OnlineUsers.Where(x => x.Value.Id == id);

            if (player is not null)
            {
                try
                {
                    Clients.Client(player.First().Key).SendAsync("GetRequestToJoinLobby", from);

                    logger.LogDebug(
                        $"{from.Username} отправил заявку на вступление в лобби игроку {player.First().Value.Username} ({player.First().Key})");
                }
                catch (Exception ex)
                {
                }
            }
        }

        // from - accepter, to - inviter
        public async Task AcceptRequestToJoinLobby(User from, User to)
        {
            if (IsPlayerInSearch(from))
            {
                Clients.Caller.SendAsync("GetActionError",
                    "Нельзя пригласить игрока, т.к. он находится в поиске матча!");
                return;
            }

            if (IsPlayerInSearch(to))
            {
                Clients.Caller.SendAsync("GetActionError",
                    "Нельзя пригласить игрока, т.к. Вы находитесь в поиске матча!");
                return;
            }

            var inviter = OnlineUsers.Where(x => x.Value.Id == to.Id);

            if (inviter is not null)
            {
                if (OnlineUsers.Where(x => x.Value.InLobbyWithPlayerID == to.Id).Count() > 2)
                {
                    Clients.Client(inviter.First().Key).SendAsync("FullLobby");
                }

                var accepter = OnlineUsers.Where(x => x.Value.Id == from.Id);

                if (accepter is not null)
                {
                    accepter.First().Value.InLobbyWithPlayerID = to.Id;
                    inviter.First().Value.InLobbyWithPlayerID = to.Id;
                }

                RefreshFriendsUsers();
                RefreshOnlineUsers();

                Clients.Client(inviter.First().Key).SendAsync("PlayerJoinedToLobby", from);

                logger.LogDebug($"{from.Username} вступил в пати к игроку {inviter.First().Value.Username}");
            }
        }

        public async Task KickPlayerFromLobby(int id)
        {
            if (IsPlayerInSearch(OnlineUsers.FirstOrDefault(x => x.Value.Id == id).Value))
            {
                Clients.Caller.SendAsync("GetActionError", "Нельзя кикнуть игрока, т.к. Вы находитесь в поиске матча!");
                return;
            }

            var player = OnlineUsers.Where(x => x.Value.Id == id);

            if (player is not null)
            {
                int previuosLobbyId = player.FirstOrDefault().Value.InLobbyWithPlayerID;

                player.FirstOrDefault().Value.InLobbyWithPlayerID = 0;

                var men = OnlineUsers.Where(x => x.Value.InLobbyWithPlayerID == previuosLobbyId);

                if (men.Count() == 1)
                {
                    men.FirstOrDefault().Value.InLobbyWithPlayerID = 0;
                }

                RefreshFriendsUsers();
                RefreshOnlineUsers();

                Clients.Client(player.FirstOrDefault().Key).SendAsync("KickedFromLobby");

                logger.LogDebug($"{player.FirstOrDefault().Value.Username} был исключен из лобби");
            }
        }

        public async Task LeaveFromLobby(User leaver)
        {
            var player = OnlineUsers.Where(x => x.Value.Id == leaver.Id);

            if (player.Any())
            {
                int previuosLobbyId = player.FirstOrDefault().Value.InLobbyWithPlayerID;

                var men = SearchUsers.Where(x => x.Value.InLobbyWithPlayerID == previuosLobbyId);

                if (previuosLobbyId != 0)
                {
                    if (men.Any())
                    {
                        foreach (var man in men)
                        {
                            StopSearch(man.Value);
                        }
                    }
                }

                player.FirstOrDefault().Value.InLobbyWithPlayerID = 0;

                men = OnlineUsers.Where(x => x.Value.InLobbyWithPlayerID == previuosLobbyId);

                if (OnlineUsers.Where(x => x.Value.Id == leaver.Id && previuosLobbyId == leaver.Id).Any())
                {
                    var destroy = OnlineUsers.Where(x => x.Value.InLobbyWithPlayerID == previuosLobbyId);

                    foreach (var item in destroy)
                    {
                        Clients.Client(item.Key).SendAsync("KickedFromLobby");
                        item.Value.InLobbyWithPlayerID = 0;
                    }
                }
                else if (men.Count() == 1)
                {
                    men.FirstOrDefault().Value.InLobbyWithPlayerID = 0;
                }

                RefreshFriendsUsers();
                RefreshOnlineUsers();

                Clients.Client(player.FirstOrDefault().Key).SendAsync("KickedFromLobby");

                logger.LogDebug($"{player.FirstOrDefault().Value.Username} вышел из лобби ({Context.ConnectionId})");
            }
        }

        #endregion

        public async Task UserConnected(User user)
        {
            if (user.SteamId == "STEAM_UNDEFINED") return;

            var connectionId = Context.ConnectionId;

            if (!OnlineUsers.ContainsKey(connectionId))
            {
                if (OnlineUsers.Where(x => x.Value.Id != 0 && x.Value.Id == user.Id).Any())
                {
                    Clients.Caller.SendAsync("AlreadyConnected");
                }
                else
                {
                    OnlineUsers[connectionId] = user;
                }
            }

            RefreshFriendsUsers();
            RefreshOnlineUsers();

            Clients.Caller.SendAsync("MixesSearchCurrentCount", SearchUsers.Count);

            logger.LogDebug($"{user.Username} подключился к хабу ({Context.ConnectionId})");
        }

        public async Task DisconnectMe(User user)
        {
            var users = OnlineUsers.Where(x => x.Value.Id == user.Id);

            if (users.Any())
            {
                var l = users.ToList();
                l.AddRange(SearchUsers.Where(x => x.Value.Id == user.Id));
                foreach (var con in users)
                {
                    try
                    {
                        Clients.Client(con.Key).SendAsync("DisconnectRequested");
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        OnlinePageUsers.TryRemove(con.Key, out int a);
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        OnlineUsers.TryRemove(con.Key, out var a);
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        SearchUsers.TryRemove(con.Key, out var a);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public async Task TimeoutUser(string connectionId)
        {
            try
            {
                FriendsPageUsers.TryRemove(connectionId, out int res);
            }
            catch (Exception)
            {
                //
            }

            try
            {
                OnlinePageUsers.TryRemove(connectionId, out int res);
            }
            catch (Exception)
            {
                //
            }

            RefreshFriendsUsers();
            RefreshOnlineUsers();

            try
            {
                OnlineUsers.TryGetValue(connectionId, out User leaver);

                logger.LogDebug($"{leaver.Username} отключился от хаба ({Context.ConnectionId})");

                LeaveFromLobby(leaver);
            }
            catch (Exception)
            {
                // null exc
            }
            finally
            {
                try
                {
                    OnlineUsers.TryRemove(connectionId, out User user);

                    if (user is null)
                    {
                    }
                    else
                    {
                        var lobbyPlayers = OnlineUsers.Where(x => x.Value.InLobbyWithPlayerID == user.Id);

                        foreach (var player in lobbyPlayers)
                        {
                            player.Value.InLobbyWithPlayerID = 0;
                            Clients.Client(player.Key).SendAsync("LobbyPlayerDisconnected");
                        }

                        SearchUsers.TryRemove(connectionId, out user);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            logger.LogDebug($"{connectionId} отключено");
        }

        public async Task UserDisconnected()
        {
            var connectionId = Context.ConnectionId;

            try
            {
                FriendsPageUsers.TryRemove(connectionId, out int res);
            }
            catch (Exception)
            {
                //
            }

            try
            {
                OnlinePageUsers.TryRemove(connectionId, out int res);
            }
            catch (Exception)
            {
                //
            }

            RefreshFriendsUsers();
            RefreshOnlineUsers();

            try
            {
                OnlineUsers.TryGetValue(connectionId, out User leaver);

                if (leaver is not null)
                {
                    logger.LogDebug($"{leaver.Username} отключился от хаба ({Context.ConnectionId})");
                }

                LeaveFromLobby(leaver);
            }
            catch (Exception)
            {
                // null exc
            }
            finally
            {
                try
                {
                    OnlineUsers.TryRemove(connectionId, out User user);

                    if (user is null)
                    {
                    }
                    else
                    {
                        var lobbyPlayers = OnlineUsers.Where(x => x.Value.InLobbyWithPlayerID == user.Id);

                        foreach (var player in lobbyPlayers)
                        {
                            player.Value.InLobbyWithPlayerID = 0;
                            Clients.Client(player.Key).SendAsync("LobbyPlayerDisconnected");
                        }

                        SearchUsers.TryRemove(connectionId, out user);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            logger.LogDebug($"{connectionId} отключено");
        }

        public async Task RemoveTimeoutClients()
        {
            while (true)
            {
                var old = OnlineUsers.Where(x => DateTime.Now.Subtract(x.Value.AliveDt).TotalMinutes > 1);

                if (old.Any())
                {
                    foreach (var user in old)
                    {
                        DisconnectMe(user.Value);
                        TimeoutUser(user.Key);
                    }
                }

                await Task.Delay(60 * 1000);
            }
        }

        public async Task SendKeepAlive(User user)
        {
            if (user is not null)
            {
                var ou = OnlineUsers.Where(x => x.Value.Id == user.Id);

                if (ou.Any())
                {
                    foreach (var u in ou)
                    {
                        u.Value.AliveDt = DateTime.Now;
                    }
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            UserDisconnected();
            base.OnDisconnectedAsync(exception);
        }
    }
}