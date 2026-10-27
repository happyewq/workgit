using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;
using ochweb.Helpers;
using ochweb.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ochweb.Controllers
{
    // 讀經名單設定：控制群組打「發」時誰會被列在未讀名單
    public class OchC040Controller : BaseController
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            if (context.Result != null) return; // 未登入，已被導回登入頁

            if (HttpContext.Session.GetString("Permission") != "Admin")
            {
                context.Result = RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> Index()
        {
            var list = new List<OchC040View>();

            using var conn = new NpgsqlConnection(DBHelper.GetConnectionString());
            await conn.OpenAsync();

            // 同一人可能有多筆「加入好友」，用 DISTINCT ON 只取一筆
            var sql = @"
                SELECT u.""UserID"", u.""UserName"",
                       COALESCE(s.""ShowUnreadYN"", 'Y') AS ""ShowUnreadYN"",
                       s.""UpdateUser"", s.""UpdateDateTime""
                FROM (
                    SELECT DISTINCT ON (""UserID"") ""UserID"", ""UserName""
                    FROM ""OCHUSER"".""linemessages""
                    WHERE ""Message"" = '加入好友'
                    ORDER BY ""UserID""
                ) u
                LEFT JOIN ""OCHUSER"".""lineuser_setting"" s ON s.""UserID"" = u.""UserID""
                ORDER BY u.""UserName"";";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new OchC040View
                {
                    UserID = reader["UserID"].ToString(),
                    UserName = reader["UserName"].ToString(),
                    ShowUnread = reader["ShowUnreadYN"].ToString() != "N",
                    UpdateUser = reader["UpdateUser"]?.ToString(),
                    UpdateDateTime = reader["UpdateDateTime"] is DateTime t ? t.ToString("yyyy/MM/dd HH:mm") : ""
                });
            }

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SetShowUnread(string userId, bool show)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "缺少 UserID" });

            try
            {
                using var conn = new NpgsqlConnection(DBHelper.GetConnectionString());
                await conn.OpenAsync();

                var sql = @"
                    INSERT INTO ""OCHUSER"".""lineuser_setting""
                        (""UserID"", ""ShowUnreadYN"", ""UpdateUser"", ""UpdateDateTime"")
                    VALUES (@UserID, @ShowYN, @UpdateUser, now())
                    ON CONFLICT (""UserID"") DO UPDATE
                    SET ""ShowUnreadYN"" = EXCLUDED.""ShowUnreadYN"",
                        ""UpdateUser"" = EXCLUDED.""UpdateUser"",
                        ""UpdateDateTime"" = now();";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@ShowYN", show ? "Y" : "N");
                cmd.Parameters.AddWithValue("@UpdateUser", (object)HttpContext.Session.GetString("UserID") ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "儲存失敗：" + ex.Message });
            }
        }
    }
}
