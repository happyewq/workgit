using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ochweb.Helpers;
using ochweb.Models;
using System;
using System.Collections.Generic;

namespace ochweb.Controllers
{
    public class OchC020Controller : BaseController
    {
        public IActionResult Index()
        {
            List<OchC020View> users = new List<OchC020View>();

            // 這裡從資料庫讀取（示意）
            string connstring = DBHelper.GetConnectionString(); // 從 appsettings.json 抓 PostgreSQL 連線字串
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = "SELECT * FROM \"OCHUSER\".\"ochsession\"";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new OchC020View
                        {
                            SessionID = Convert.ToInt32(reader["SessionID"]),
                            SessionName = reader["SessionName"].ToString(),
                            SessionContent = reader["SessionContent"].ToString(),
                            SessionLocation = reader["SessionLocation"].ToString(),
                            StartTime = FormatDateTime(reader["StartTime"].ToString()),
                            EndTime = FormatDateTime(reader["EndTime"].ToString()),
                            CreateDateTime = FormatDateTime(reader["CreateDateTime"].ToString())
                        });
                    }
                }
            }

            return View(users);
        }

        public IActionResult Details(int id)
        {
            var model = new SessionDetailViewModel();
            model.SessionID = id;
            model.Registrants = new List<SessionDetailViewModel.RegistrantInfo>();

            string connstring = DBHelper.GetConnectionString();
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();

                // 先查場次名稱
                using (var cmd = new NpgsqlCommand("SELECT \"SessionName\" FROM \"OCHUSER\".\"ochsession\" WHERE \"SessionID\" = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            model.SessionName = reader["SessionName"].ToString();
                    }
                }

                // 再查報名人員
                string sql = @"SELECT * FROM ""OCHUSER"".""ochregist"" WHERE ""SessionID"" = @id";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Registrants.Add(new SessionDetailViewModel.RegistrantInfo
                            {
                                UserID = reader["UserID"].ToString(),
                                UserType = reader["UserType"].ToString(),
                                FeeAmount = Convert.ToDecimal(reader["FeeAmount"]),
                                PaidYN = reader["PaidYN"].ToString(),
                                RegisterTime = reader["RegisterTime"].ToString()
                            });
                        }
                    }
                }
            }

            return View(model);
        }

        private string FormatDateTime(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Length < 12) return raw;

            string year = raw.Substring(0, 4);
            string month = raw.Substring(4, 2);
            string day = raw.Substring(6, 2);
            string hour = raw.Substring(8, 2);
            string minute = raw.Substring(10, 2);

            return $"{year}/{month}/{day} {hour}:{minute}";
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            string connstring = DBHelper.GetConnectionString();
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = "DELETE FROM \"OCHUSER\".\"ochsession\" WHERE \"SessionID\" = @id";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok();
        }

        [HttpPost]
        public IActionResult SaveData(OchC020View model)
        {
            if (!ModelState.IsValid) return View(model);

            string startTime14 = ConvertTo14Char(model.StartTime);
            string endTime14 = ConvertTo14Char(model.EndTime);

            string connstring = DBHelper.GetConnectionString();
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = @"UPDATE ""OCHUSER"".""ochsession"" 
                       SET ""SessionName"" = @name,
                           ""SessionContent"" = @content,
                           ""SessionLocation"" = @location,
                           ""StartTime"" = @start,
                           ""EndTime"" = @end
                       WHERE ""SessionID"" = @id";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", model.SessionName);
                    cmd.Parameters.AddWithValue("@content", model.SessionContent);
                    cmd.Parameters.AddWithValue("@location", model.SessionLocation);
                    cmd.Parameters.AddWithValue("@start", startTime14);
                    cmd.Parameters.AddWithValue("@end", endTime14);
                    cmd.Parameters.AddWithValue("@id", model.SessionID);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
        // 將 yyyy-MM-ddTHH:mm 格式轉成 yyyyMMddHHmmss
        private string ConvertTo14Char(string input)
        {
            if (DateTime.TryParse(input, out DateTime dt))
            {
                return dt.ToString("yyyyMMddHHmmss");
            }
            return input; // 如果轉換失敗，就原樣寫入
        }


        [HttpPost]
        public IActionResult CreateData(OchC020View model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string startTime14 = ConvertTo14Char(model.StartTime);
            string endTime14 = ConvertTo14Char(model.EndTime);
            string createTime14 = DateTime.Now.ToString("yyyyMMddHHmmss");

            string connstring = DBHelper.GetConnectionString();
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = @"
            INSERT INTO ""OCHUSER"".""ochsession"" 
            (""SessionName"", ""SessionContent"", ""SessionLocation"", ""StartTime"", ""EndTime"", ""CreateDateTime"")
            VALUES (@name, @content, @location, @start, @end, @create)";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", model.SessionName);
                    cmd.Parameters.AddWithValue("@content", model.SessionContent ?? "");
                    cmd.Parameters.AddWithValue("@location", model.SessionLocation ?? "");
                    cmd.Parameters.AddWithValue("@start", startTime14);
                    cmd.Parameters.AddWithValue("@end", endTime14);
                    cmd.Parameters.AddWithValue("@create", createTime14);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = new OchC020View();

            string connstring = DBHelper.GetConnectionString();
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = @"SELECT * FROM ""OCHUSER"".""ochsession"" WHERE ""SessionID"" = @id";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.SessionID = id;
                            model.SessionName = reader["SessionName"].ToString();
                            model.SessionContent = reader["SessionContent"].ToString();
                            model.SessionLocation = reader["SessionLocation"].ToString();
                            model.StartTime = reader["StartTime"].ToString();
                            model.StartTime = FormatForDatetimeLocal(reader["StartTime"].ToString());
                            model.EndTime = FormatForDatetimeLocal(reader["EndTime"].ToString());
                        }
                    }
                }
            }

            return View(model);
        }
        private string FormatForDatetimeLocal(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Length < 12) return "";

            try
            {
                // 例：20250508153000 → 2025-05-08T15:30
                var dt = DateTime.ParseExact(raw.Substring(0, 12), "yyyyMMddHHmm", null);
                return dt.ToString("yyyy-MM-ddTHH:mm");
            }
            catch
            {
                return "";
            }
        }


        [HttpGet]
        public IActionResult Create()
        {
            var model = new OchC020View();

            return View(model);
        }
    }
}
