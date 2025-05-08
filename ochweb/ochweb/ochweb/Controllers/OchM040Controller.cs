using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ochweb.Helpers;
using ochweb.Models;
using System;
using System.Collections.Generic;
using Dapper;
using System.Data;

namespace ochweb.Controllers
{
	public class OchM040Controller : BaseController
    {
        public IActionResult Index()
        {
            List<OchM040View> users = new List<OchM040View>();

            // 這裡從資料庫讀取（示意）
            string connstring = DBHelper.GetConnectionString(); // 從 appsettings.json 抓 PostgreSQL 連線字串
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = "SELECT * FROM \"OCHUSER\".\"ochuser\"";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new OchM040View
                        {
                            UserID = reader["UserID"].ToString(),
                            UserNMC = reader["UserNMC"].ToString(),
                            Permission = reader["Permission"].ToString(),
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
        public JsonResult SaveUser(OchM040View OchM040View)
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
                        (""UserID"", ""UserNMC"", ""Password"", ""DCcode"", ""CancelYN"", ""CreateDateTime"", ""Permission"")
                        VALUES (@UserID, @UserNMC, @Password, '1', 'N', @CreateDateTime, @Permission)
                    ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", OchM040View.UserID);
                        cmd.Parameters.AddWithValue("@UserNMC", OchM040View.UserNMC);
                        cmd.Parameters.AddWithValue("@Password", OchM040View.Password);
                        cmd.Parameters.AddWithValue("@CreateDateTime", DateTime.Now.ToString("yyyyMMdd"));
                        cmd.Parameters.AddWithValue("@Permission", OchM040View.Permission);

                        cmd.ExecuteNonQuery();

                        result.UserID = OchM040View.UserID;
                        result.UserNMC = OchM040View.UserNMC;
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
        public ActionResult Edit(string id)
        {
            string connstring = DBHelper.GetConnectionString();
            OchM040View user = null;

            using (var conn = new NpgsqlConnection(connstring))
            {
                string sql = @"SELECT * FROM ""OCHUSER"".""ochuser"" WHERE ""UserID"" = @UserID";
                user = conn.QueryFirstOrDefault<OchM040View>(sql, new { UserID = id });
            }

            if (user == null)
                return NotFound();

            return View("Edit", user);
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

        [HttpPost]
        public JsonResult SaveData(OchM040View user)
        {
            var result = new OchM040View();

            try
            {
                string connstring = DBHelper.GetConnectionString();

                using (var conn = new NpgsqlConnection(connstring))
                {
                    conn.Open();

                    // 動態組 SQL
                    string sql = @"
                UPDATE ""OCHUSER"".""ochuser"" 
                SET ""UserNMC"" = @UserNMC";


                    if (!string.IsNullOrWhiteSpace(user.NewPassword))
                    {
                        sql += ", \"Password\" = @Password";
                    }

                    if (!string.IsNullOrWhiteSpace(user.Permission))
                    {
                        sql += ", \"Permission\" = @Permission";
                    }

                    sql += " WHERE \"UserID\" = @UserID";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserNMC", user.UserNMC);
                        cmd.Parameters.AddWithValue("@UserID", user.UserID);

                        if (!string.IsNullOrWhiteSpace(user.NewPassword))
                        {
                            cmd.Parameters.AddWithValue("@Password", user.NewPassword);
                        }

                        if (!string.IsNullOrWhiteSpace(user.Permission))
                        {
                            cmd.Parameters.AddWithValue("@Permission", user.Permission);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                result.UserID = user.UserID;
                result.UserNMC = user.UserNMC;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "更新失敗：" + ex.Message;
            }

            return Json(result);
        }

    }
}
