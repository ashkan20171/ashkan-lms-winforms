using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Serialization.Json;

namespace AshkanLMS.Services
{
    // Optional SQL Server persistence for the complete LMS state. Uses a single atomic snapshot.
    // Never embeds passwords or connection strings in source code.
    internal sealed class SqlStateStore
    {
        private readonly string connectionString;
        public SqlStateStore(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string required.");
            this.connectionString = connectionString;
        }

        public LmsState Load()
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand("SELECT StateJson FROM dbo.LmsState WHERE Id = @id", connection))
            {
                command.Parameters.Add("@id", SqlDbType.Int).Value = 1;
                connection.Open();
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes((string)result)))
                    return (LmsState)new DataContractJsonSerializer(typeof(LmsState)).ReadObject(stream);
            }
        }

        public void Save(LmsState state)
        {
            string json;
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(LmsState)).WriteObject(stream, state);
                json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        const string sql = @"UPDATE dbo.LmsState SET StateJson=@json, UpdatedUtc=SYSUTCDATETIME() WHERE Id=@id;
IF @@ROWCOUNT=0 INSERT INTO dbo.LmsState (Id,StateJson,UpdatedUtc) VALUES (@id,@json,SYSUTCDATETIME());";
                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.Add("@id", SqlDbType.Int).Value = 1;
                            command.Parameters.Add("@json", SqlDbType.NVarChar, -1).Value = json;
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
            }
        }
    }
}
