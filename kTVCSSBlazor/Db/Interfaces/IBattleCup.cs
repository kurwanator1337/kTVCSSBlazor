using kTVCSSBlazor.Db.Models.BattleCup;

namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IBattleCup
    {
        List<BattleCup> GetCup();
        BattleCup CreateBattleCup();
        BattleCup GetBattleCup(int id);
        void AddPlayerToBattleCup(int id, int bid);
        void RemovePlayerFromBattleCup(int id, int bid);
        void DeleteBattleCupNotStarted(int bid);
        void CreateMatch(Match match, int bid, Part part);
        bool GetResults(int bid, Part part);
        void FinishBattleCup(int bid);
    }
}
