using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ochweb.Helpers;
using ochweb.Models;
using System;
using System.Collections.Generic;

namespace ochweb.Controllers
{
	public class OchM040Controller : Controller
	{
        public IActionResult Index()
        {
            List<OchM040View> users = new List<OchM040View>();

            // 這裡從資料庫讀取（示意）
            string connstring = DBHelper.GetConnectionString(); // 從 appsettings.json 抓 PostgreSQL 連線字串
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = "SELECT \"UserID\", \"UserNMC\", \"Password\", \"CreateDateTime\" FROM \"OCHUSER\".\"ochuser\"";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new OchM040View
                        {
                            UserID = reader["UserID"].ToString(),
                            UserNMC = reader["UserNMC"].ToString(),
                            Password = "", // 不回傳密碼
                            CreateDateTime = reader["CreateDateTime"].ToString()
                        });
                    }
                }
            }

            return View(users);
        }
        public ActionResult AddUser()
        {
            //
            return View("Add");
        }

        [HttpPost]
        public JsonResult SaveUser(string userID, string userNMC, string password)
        {
            var result = new ochweb.Models.OchM040View();

            try
            {
                 string connstring = DBHelper.GetConnectionString(); // 從 appsettings.json 抓 PostgreSQL 連線字串

                using (var conn = new NpgsqlConnection(connstring))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO ""OCHUSER"".""ochuser""
                        (""UserID"", ""UserNMC"", ""Password"", ""DCcode"", ""CancelYN"", ""CreateDateTime"")
                        VALUES (@UserID, @UserNMC, @Password, '1', 'N', @CreateDateTime)
                    ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userID);
                        cmd.Parameters.AddWithValue("@UserNMC", userNMC);
                        cmd.Parameters.AddWithValue("@Password", password);
                        cmd.Parameters.AddWithValue("@CreateDateTime", DateTime.Now.ToString("yyyyMMdd"));

                        cmd.ExecuteNonQuery();

                        result.UserID = userID;
                        result.UserNMC = userNMC;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "新增失敗：" + ex.Message;
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult DeleteUser(string userID)
        {
            var result = new OchM040View();

            try
            {
                string connstring = DBHelper.GetConnectionString(); // 從 appsettings.json 抓 PostgreSQL 連線字串

                using (var conn = new NpgsqlConnection(connstring))
                {
                    conn.Open();

                    string sql = @"DELETE FROM ""OCHUSER"".""ochuser"" WHERE ""UserID"" = @UserID";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userID);

                        int affected = cmd.ExecuteNonQuery();

                        if (affected == 0)
                        {
                            result.ErrorMessage = "找不到該使用者";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "刪除失敗：" + ex.Message;
            }

            return Json(result);
        }

    }
}
