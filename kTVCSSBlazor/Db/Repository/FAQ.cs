using Dapper;
using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Models.FAQ;
using System.Data;
using System.Data.SqlClient;

namespace kTVCSSBlazor.Db.Repository
{
    public class FAQ(IConfiguration configuration, ILogger logger) : Context(configuration, logger), IFAQ
    {
        public List<Model> Get()
        {
            EnsureConnected();

            var data = Db.Query<Model>("SELECT * FROM FAQ");

            return data.ToList();
        }
    }
}
