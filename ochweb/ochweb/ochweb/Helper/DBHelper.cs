using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace ochweb.Helpers
{
	public static class DBHelper
	{
		private static string _connectionString;

		public static void Init(IConfiguration configuration)
		{
			_connectionString = configuration.GetConnectionString("DefaultConnection");
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
