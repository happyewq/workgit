using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;

namespace ochweb.Helpers
{
	public static class DBHelper
	{
		private static string _connectionString;


        public static void Init(IConfiguration configuration)
        {
            // 改為取環境變數
            _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

            // 可選：如果環境變數沒有設定，就 fallback 到 appsettings.json
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection");
            }
        }

        public static string GetConnectionString()
        {
            return _connectionString;
        }
        public static SqlConnection GetConnection()
		{
			return new SqlConnection(_connectionString);
		}
	}
}
