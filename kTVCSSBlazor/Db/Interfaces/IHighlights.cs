using kTVCSSBlazor.Db.Models.Highlights;
namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IHighlights
    {
        Task<List<Result>> GetByPlayer(int id, string requester);
        Task<List<Result>> GetByMatch(int id, string steam);
    }
}
