using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ochweb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using ochweb.Helpers;
using System.Linq;

namespace ochweb.Controllers
{
    public class OchC030Controller : Controller
    {
        private readonly IConfiguration _config;
        public OchC030Controller(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-2); // 預設查近3天
            var end = endDate ?? DateTime.Today;

            var model = new BibleLogViewModel
            {
                StartDate = start,
                EndDate = end
            };

            for (var d = start; d <= end; d = d.AddDays(1))
                model.DateList.Add(d.ToString("yyyyMMdd"));

            var connStr = DBHelper.GetConnectionString();
            using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();

            // 先抓所有加入好友的用戶
            var users = new Dictionary<string, string>(); // userID -> userName

            var cmdUsers = new NpgsqlCommand(@"
        SELECT DISTINCT ""UserID"", ""UserName""
        FROM ""OCHUSER"".""linemessages""
        WHERE ""Message"" = '加入好友';
    ", conn);

            using var reader1 = await cmdUsers.ExecuteReaderAsync();
            while (await reader1.ReadAsync())
            {
                users[reader1.GetString(0)] = reader1.GetString(1);
            }
            reader1.Close();

            // 再查詢這段期間所有讀經紀錄
            var cmdBible = new NpgsqlCommand(@"
        SELECT ""UserID"", ""CreateDateTime""
        FROM ""OCHUSER"".""ochbible""
        WHERE ""CreateDateTime"" BETWEEN @start AND @end;
    ", conn);

            cmdBible.Parameters.AddWithValue("@start", start.ToString("yyyyMMdd"));
            cmdBible.Parameters.AddWithValue("@end", end.ToString("yyyyMMdd"));

            var userMap = new Dictionary<string, BibleLogRecord>();

            using var reader2 = await cmdBible.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                var userId = reader2.GetString(0);
                var dateStr = reader2.GetString(1);

                if (!userMap.ContainsKey(userId))
                {
                    userMap[userId] = new BibleLogRecord
                    {
                        UserID = userId,
                        UserName = users.ContainsKey(userId) ? users[userId] : "(未知)",
                        DailyReadMap = model.DateList.ToDictionary(d => d, _ => false)
                    };
                }

                if (userMap[userId].DailyReadMap.ContainsKey(dateStr))
                {
                    userMap[userId].DailyReadMap[dateStr] = true;
                }
            }

            // 把沒讀的也補上（所有 user 要顯示）
            foreach (var u in users)
            {
                if (!userMap.ContainsKey(u.Key))
                {
                    userMap[u.Key] = new BibleLogRecord
                    {
                        UserID = u.Key,
                        UserName = u.Value,
                        DailyReadMap = model.DateList.ToDictionary(d => d, _ => false)
                    };
                }
            }

            model.Records = userMap.Values.ToList();

            return View(model);
        }


    }
}
