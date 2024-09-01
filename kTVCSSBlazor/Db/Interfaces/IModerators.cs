namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IModerators
    {
        Task<bool> IsModerator(int id);
        Task SetTier(string moderator, int id, int tier);
    }
}
