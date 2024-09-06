namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IVips
    {
        Task<bool> ResetStats(int id);
        Task<bool> ResetFullStats(int id);
        Task<bool> IsVip(string steam);
        Task<bool> IsPremiumVip(string steam);
    }
}
