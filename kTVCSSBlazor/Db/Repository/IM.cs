using Dapper;
using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Models.IM;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

namespace kTVCSSBlazor.Db.Repository
{
    public class IM(string connectionString) : IIM
    {
        private SqlConnection Db { get; set; } = new SqlConnection(connectionString);
        private string ConnectionString { get; set; } = connectionString;

        public List<DialogItem> GetDialogs(int id)
        {
            List<DialogItem> dialogs = new();

            EnsureConnected();

            dialogs.AddRange(Db.Query<DialogItem>($"SELECT DISTINCT P.ID as Id, P.LOGIN as Name, P.PHOTO as Avatar, DateTime FROM IM INNER JOIN Players AS P WITH (NOLOCK) ON P.ID = IM.FromID WHERE IM.ToID = {id}"));
            dialogs.AddRange(Db.Query<DialogItem>($"SELECT DISTINCT P.ID as Id, P.LOGIN as Name, P.PHOTO as Avatar, DateTime FROM IM INNER JOIN Players AS P WITH (NOLOCK) ON P.ID = IM.ToID WHERE IM.FromID = {id}"));

            return dialogs.OrderByDescending(x => x.DateTime).DistinctBy(x => x.ID).ToList();
        }

        public List<Message> GetMessages(int from, int to)
        {
            List<Message> messages = new();

            EnsureConnected();

            DynamicParameters d = new DynamicParameters();
            d.Add("from", from);
            d.Add("to", to);

            messages = Db.Query<Message>("GetIM", d, commandType: CommandType.StoredProcedure).ToList();

            SetReaded(messages);
            
            return messages;
        }

        private async Task SetReaded(List<Message> messages)
        {
            EnsureConnected();

            var temp = messages.DistinctBy(x => x.FromID);
            temp = messages.DistinctBy(x => x.ToID);
            temp = messages.DistinctBy(x => x.Text);

            foreach (var message in temp)
            {
                await Db.ExecuteAsync($"UPDATE IM SET READED = 1 WHERE FromID = {message.FromID} AND ToID = {message.ToID}");
            }
        }

        public void SendMessage(int from, int to, string message)
        {
            EnsureConnected();

            DynamicParameters d = new DynamicParameters();

            d.Add("text", message);
            d.Add("from", from);
            d.Add("to", to);

            Db.Execute("AddIM", d, commandType: CommandType.StoredProcedure);
        }

        public int GetUnreadedCount(int id)
        {
            EnsureConnected();

            return Db.QueryFirst<int>(
                $"SELECT COUNT(*) FROM IM INNER JOIN Players AS P WITH (NOLOCK) ON P.ID = IM.FromID INNER JOIN Players AS P2 WITH (NOLOCK) ON P2.ID = IM.ToID WHERE ToID = {id} AND READED = 0");
        }

        public async Task SetMessageReaded(Message message)
        {
            EnsureConnected();

            Db.Execute($"UPDATE IM SET READED = 1 WHERE FromID = {message.FromID} AND ToID = {message.ToID}");
        }

        private void EnsureConnected()
        {
            try
            {
                if (Db.State != ConnectionState.Open)
                {
                    Db = new SqlConnection(Db.ConnectionString);
                    Db.Open();
                }
            }
            catch (Exception)
            {
                Db = new SqlConnection(ConnectionString);
                EnsureConnected();
            }
        }
    }
}
