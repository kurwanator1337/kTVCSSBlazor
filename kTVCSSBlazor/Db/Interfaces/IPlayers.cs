using kTVCSSBlazor.Db.Models.Players;
using System.IO.Pipelines;

namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IPlayers
    {
        List<TotalPlayer> Get();
        List<BannedUser> GetBannedList();
        Models.Players.Player Get(int id);
        Models.Players.Player Get(string steam);
        PlayerInfo GetPlayerInfo(int id);
        Profile GetProfile(int id);
        string SaveProfile(Profile profile, int id);
        List<Fft> GetFftList();
        bool AddFriend(int player, int target);
        void RemoveFriend(int player, int target);
        List<FriendRequest> GetFriendRequests(int player);
        List<FriendRequest> GetFriendRequestsOutgoing(int player);
        void AcceptFriendRequest(int player, int target);
        void RemoveFriendRequest(int player, int target);
        bool IsFriend(int player, int target);
        List<TotalPlayer> GetFriends(int player);
        List<TotalPlayer> GetOnlinePlayers();
        void MakeReport(int me, int player, string text);
        Task<bool> IsVip(string steam);
        string GetAfterSignupMessage(int id);
        string GetTelegramID(int id);
        void SavePlayerAchiviments(int id, string achvs);
        void SetLCReaded(int id);
        Task<List<Alert>> GetAlerts(int id);
        void SupressAlerts(int id);
        bool HasCam(int id);
        List<Cam> GetCamHistory(int id);
        void SaveCam(Cam cam);
    }
}
