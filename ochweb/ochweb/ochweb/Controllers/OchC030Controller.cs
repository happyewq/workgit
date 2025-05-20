using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ochweb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace ochweb.Controllers
{
    public class OchC030Controller : Controller
    {
        private readonly IConfiguration _config;
        public OchC030Controller(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index(DateTime? date)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");
            var model = new BibleLogViewModel
            {
                QueryDate = date ?? DateTime.Today,
                Records = new List<BibleLogRecord>()
            };

            var dateStr = model.QueryDate.Value.ToString("yyyyMMdd");

            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
            SELECT m.""UserID"", m.""UserName"",
                   CASE WHEN b.""UserID"" IS NOT NULL THEN true ELSE false END AS ""HasRead""
            FROM ""OCHUSER"".""linemessages"" m
            LEFT JOIN (
                SELECT DISTINCT ""UserID""
                FROM ""OCHUSER"".""ochbuible""
                WHERE ""CreateDateTime"" = @today
            ) b ON m.""UserID"" = b.""UserID""
            WHERE m.""Message"" = '加入好友'
        ", conn);
            cmd.Parameters.AddWithValue("@today", dateStr);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                model.Records.Add(new BibleLogRecord
                {
                    UserID = reader.GetString(0),
                    UserName = reader.GetString(1),
                    HasRead = reader.GetBoolean(2)
                });
            }

            return View(model);
        }
    }
}
