namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IAdmins
    {
        Task<bool> IsAdmin(string steam);
        Task BlockTeam(int id, string admin);
        Task UnBlockTeam(int id, string admin);
        Task UnbanPlayer(int id, string admin, string reason);
        Task SetMMR(int id, string admin, int mmr);
        Task UnbanRequest(int id, string reason);
        Task BanPlayer(int id, string reason, string admin);
        Task SetServerMix(int id, string admin);
        Task SetServerMatch(int id, string admin);
        Task StopMatch(int id, string admin);
        Task RestartAllNodes(string admin);
        Task SetModerator(int id, string admin);
        Task RemoveModerator(int id, string admin);
        Task BugReport(int id, string text);
        Task RestoreMMR();
    }
}
