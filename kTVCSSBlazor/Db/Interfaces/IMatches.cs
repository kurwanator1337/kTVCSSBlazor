using kTVCSSBlazor.Db.Models.Matches;

namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IMatches
    {
        List<TotalMatch> GetTotalMatches();
        List<TotalMatch> GetMyBestMatches(string steam);
        MatchInfo GetMatchByID(int id);
        string GetSourceTV(int id);
    }
}
