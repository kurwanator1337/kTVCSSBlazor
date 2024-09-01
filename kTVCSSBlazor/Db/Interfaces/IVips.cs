namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IVips
    {
        Task<bool> ResetStats(int id);
        Task<bool> IsVip(string steam);
    }
}
