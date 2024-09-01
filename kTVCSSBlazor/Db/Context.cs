using Dapper;
using System.Data.SqlClient;
using System.Data;

namespace kTVCSSBlazor.Db
{
    public class Context(string connectionString)
    {
        public SqlConnection Db { get; set; } = new SqlConnection(connectionString);
        public string ConnectionString { get; set; } = connectionString;

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

        public async Task ExecuteWithNoReturn(string query)
        {
            EnsureConnected();

            Db.ExecuteAsync(query);
        }

        public async Task<T> ReadSingleRecord<T>(string query)
        {
            EnsureConnected();

            try
            {
                return await Db.QuerySingleOrDefaultAsync<T>(query);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public async Task ExecuteStoredProcedure(string spName, DynamicParameters dynamicParameters)
        {
            EnsureConnected();

            Db.ExecuteAsync(spName, dynamicParameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<T> ExecuteStoredProcedureWithReturnValue<T>(string spName, DynamicParameters dynamicParameters) where T : new()
        {
            EnsureConnected();

            dynamicParameters.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            await Db.ExecuteAsync(spName, dynamicParameters, commandType: CommandType.StoredProcedure);
            return dynamicParameters.Get<T>("ReturnValue");
        }

        public async Task<IEnumerable<T>> ExecuteStoredProcedureWithNoParameters<T>(string spName) where T : new()
        {
            EnsureConnected();

            return await Db.QueryAsync<T>(spName, commandType: CommandType.StoredProcedure);
        }
    }
}
