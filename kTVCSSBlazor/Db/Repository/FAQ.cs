using Dapper;
using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Models.FAQ;
using System.Data;
using System.Data.SqlClient;

namespace kTVCSSBlazor.Db.Repository
{
    public class FAQ(string connectionString) : IFAQ
    {
        private SqlConnection Db { get; set; } = new SqlConnection(connectionString);
        private string ConnectionString { get; set; } = connectionString;

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

        public List<Model> Get()
        {
            EnsureConnected();

            var data = Db.Query<Model>("SELECT * FROM FAQ");

            return data.ToList();
        }
    }
}
