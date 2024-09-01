using kTVCSSBlazor.Db.Models.Teams;

namespace kTVCSSBlazor.Db.Interfaces
{
    public interface ITeams
    {
        List<Team> Get();
        TeamPageData GetTeamByID(int id);
        int DeleteTeam(int id, string steam);
        int LeaveTeam(string steam);
        int KickFromTeam(string steam);
        int MakeInvite(string captain, string invited);
        string CheckInvite(string steam);
        void DeclineInvite(string steam);
        int AcceptInvite(string steam);
        bool IsPlayerCaptain(string steam);
        int CreateTeam(string name, string steam);
        int GetTeamBySteamID(string steam);
        TeamEdit GetTeamForEdit(int id);
        int SaveTeam(TeamEdit team);
        void SaveTeamAchiviments(int id, string achvs);
        Task TeamsAutoDeletion();
    }
}
