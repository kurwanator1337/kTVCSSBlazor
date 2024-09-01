using kTVCSSBlazor.Db.Models.IM;

namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IIM
    {
        List<DialogItem> GetDialogs(int id);
        List<Message> GetMessages(int from, int to);
        void SendMessage(int from, int to, string message);
        int GetUnreadedCount(int id);
        Task SetMessageReaded(Message message);
    }
}
