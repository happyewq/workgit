using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
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
                string sql = @"SELECT * 
                               FROM ""OCHUSER"".""ochregist"" 
                               WHERE ""SessionID"" = @id 
                               ORDER BY ""PaidYN"" ASC";
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
                                UserNMC = reader["UserNMC"].ToString(),
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

        // GET: /YourController/Create?sessionId=123
        [HttpGet]
        public IActionResult CreateDetail(int sessionId)
        {
            // 也可以在這裡順手查 SessionName 顯示在畫面上（若Create頁要顯示）
            var vm = new RegistrantCreateViewModel
            {
                SessionID = sessionId,
                RegisterTime = DateTime.Now,
                UserType = "w",
                PaidYN = "N",
                FeeAmount = 0
            };
            return View(vm);
        }

        // POST: /YourController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveDetailData(RegistrantCreateViewModel vm)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View(vm);
            //}

            string connstring = DBHelper.GetConnectionString();
            try
            {
                using (var conn = new NpgsqlConnection(connstring))
                {
                    conn.Open();

                    // 若資料表有 NOT NULL 的 RegisterTime，可傳入參數；若 DB 有預設 NOW()，也可不傳此欄位
                    string sql = @"
                INSERT INTO ""OCHUSER"".""ochregist""
                    (""SessionID"", ""UserID"", ""UserNMC"", ""UserType"", ""FeeAmount"", ""PaidYN"", ""CancelYN"", ""RegisterTime"")
                VALUES
                    (@SessionID, @UserID, @UserNMC, @UserType, @FeeAmount, @PaidYN, @CancelYN, @RegisterTime);
            ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@SessionID", NpgsqlDbType.Integer).Value = vm.SessionID;
                        cmd.Parameters.Add("@UserID", NpgsqlDbType.Varchar).Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                        cmd.Parameters.Add("@UserNMC", NpgsqlDbType.Varchar).Value = vm.UserNMC?.Trim() ?? string.Empty;
                        cmd.Parameters.Add("@UserType", NpgsqlDbType.Varchar).Value = vm.UserType?.Trim() ?? "w";
                        cmd.Parameters.Add("@FeeAmount", NpgsqlDbType.Numeric).Value = vm.FeeAmount;
                        cmd.Parameters.Add("@PaidYN", NpgsqlDbType.Char).Value = (vm.PaidYN == "Y" ? "Y" : "N");
                        cmd.Parameters.Add("@PaidYN", NpgsqlDbType.Char).Value = (vm.PaidYN == "Y" ? "Y" : "N");
                        cmd.Parameters.Add("@CancelYN", NpgsqlDbType.Char).Value = "N";
                        cmd.Parameters.Add("@RegisterTime", NpgsqlDbType.Timestamp).Value = vm.RegisterTime ?? DateTime.Now;

                        cmd.ExecuteNonQuery();
                    }
                }

                // 新增完成，導回 Details
                return RedirectToAction(nameof(Details), new { id = vm.SessionID });
            }
            catch (Exception ex)
            {
                // 你也可以把 ex 記錄到 Log
                ModelState.AddModelError(string.Empty, $"新增失敗：{ex.Message}");
                return View(vm);
            }
        }


        // GET: /YourController/Edit/USER123?sessionId=456
        [HttpGet]
        public IActionResult EditDetail(string id, int sessionId)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var vm = new RegistrantEditViewModel();

            string connstring = DBHelper.GetConnectionString();
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();

                string sql = @"
            SELECT ""SessionID"", ""UserID"", ""UserNMC"", ""UserType"", ""FeeAmount"", ""PaidYN"", ""RegisterTime""
            FROM ""OCHUSER"".""ochregist""
            WHERE ""SessionID"" = @SessionID AND ""UserID"" = @UserID
            LIMIT 1;";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@SessionID", NpgsqlDbType.Integer).Value = sessionId;
                    cmd.Parameters.Add("@UserID", NpgsqlDbType.Varchar).Value = id;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return NotFound();

                        vm.SessionID = reader.GetInt32(reader.GetOrdinal("SessionID"));
                        vm.UserID = reader["UserID"].ToString();
                        vm.UserNMC = reader["UserNMC"].ToString();
                        vm.UserType = reader["UserType"].ToString();
                        vm.FeeAmount = Convert.ToDecimal(reader["FeeAmount"]);
                        vm.PaidYN = reader["PaidYN"].ToString();
                        // RegisterTime 可能是 timestamp or null
                        vm.RegisterTime = reader["RegisterTime"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(reader["RegisterTime"]);
                    }
                }
            }

            return View(vm);
        }

        // POST: /YourController/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveEditDetail(RegistrantEditViewModel vm)
        {
            //if (!ModelState.IsValid)
            //    return View(vm);

            string connstring = DBHelper.GetConnectionString();
            try
            {
                using (var conn = new NpgsqlConnection(connstring))
                {
                    conn.Open();

                    string sql = @"     UPDATE ""OCHUSER"".""ochregist""
                                          SET ""UserNMC"" = @UserNMC,
                                              ""UserType"" = @UserType,
                                              ""FeeAmount"" = @FeeAmount,
                                              ""PaidYN"" = @PaidYN,
                                              ""RegisterTime"" = @RegisterTime
                                        WHERE ""SessionID"" = @SessionID
                                          AND ""UserID"" = @UserID;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@UserNMC", NpgsqlDbType.Varchar).Value = vm.UserNMC?.Trim() ?? string.Empty;
                        cmd.Parameters.Add("@UserType", NpgsqlDbType.Varchar).Value = vm.UserType?.Trim() ?? "w";
                        cmd.Parameters.Add("@FeeAmount", NpgsqlDbType.Numeric).Value = vm.FeeAmount;
                        cmd.Parameters.Add("@PaidYN", NpgsqlDbType.Char).Value = (vm.PaidYN == "Y" ? "Y" : "N");
                        cmd.Parameters.Add("@RegisterTime", NpgsqlDbType.Timestamp).Value = (object?)(vm.RegisterTime ?? DateTime.Now);

                        cmd.Parameters.Add("@SessionID", NpgsqlDbType.Integer).Value = vm.SessionID;
                        cmd.Parameters.Add("@UserID", NpgsqlDbType.Varchar).Value = vm.UserID;

                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 0)
                        {
                            ModelState.AddModelError(string.Empty, "找不到要更新的資料（可能已被刪除）。");
                            return View(vm);
                        }
                    }
                }

                // 回到 Details
                return RedirectToAction(nameof(Details), new { id = vm.SessionID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"更新失敗：{ex.Message}");
                return View(vm);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDetail(string id, int sessionId)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            string connstring = DBHelper.GetConnectionString();
            try
            {
                using (var conn = new NpgsqlConnection(connstring))
                {
                    conn.Open();

                    string sql = @"  DELETE FROM ""OCHUSER"".""ochregist""
                                     WHERE ""SessionID"" = @SessionID
                                       AND ""UserID"" = @UserID;
                                     ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@SessionID", NpgsqlDbType.Integer).Value = sessionId;
                        cmd.Parameters.Add("@UserID", NpgsqlDbType.Varchar).Value = id;

                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 0)
                        {
                            TempData["ErrorMessage"] = "刪除失敗，找不到這筆報名者資料。";
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "刪除成功！";
                        }
                    }
                }

                // 刪除後回到該場次詳細
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"刪除時發生錯誤：{ex.Message}";
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }
        }
    }
}
