using kTVCSSBlazor.Db.Models.BattleCup;

namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IBattleCup
    {
        List<BattleCup> GetCups();
        BattleCup CreateBattleCup();
        BattleCup GetBattleCup(int id);
        void AddTeamToBattleCup(int team, int bid);
        void RemoveTeamFromBattleCup(int team, int bid);
        void DeleteBattleCupNotStarted();
        void CreateMatch(Match match, int bid, Part part);
        bool GetResults(int bid, Part part);
        void FinishBattleCup(int bid);
    }
}
