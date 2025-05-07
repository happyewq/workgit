using Microsoft.AspNetCore.Mvc;
using ochweb.Models;
using ochweb.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Npgsql;
using Microsoft.AspNetCore.Http;

namespace ochweb.Controllers
{
	public class LoginController : Controller
	{
		public ActionResult Index()
		{
			LoginView NewLoginView = new LoginView();
			return View("Index", NewLoginView);
		}

        [HttpPost]
        public ActionResult UserLogin(string UserID, string Password)
        {
            LoginView Result = new LoginView();

            try
            {
                string connstring = DBHelper.GetConnectionString();

                using (var conn = new NpgsqlConnection(connstring))
                {
                    conn.Open();

                    string sql = "SELECT * FROM \"OCHUSER\".\"ochuser\" WHERE \"UserID\" = @UserID AND \"Password\" = @Password";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", UserID);
                        cmd.Parameters.AddWithValue("@Password", Password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // 設定回傳資料
                                Result.UserNMC = reader["UserNMC"].ToString();
                                Result.UserID = reader["UserID"].ToString();
                                Result.Permission = reader["Permission"].ToString();

                                // ✅ 寫入 Session
                                HttpContext.Session.SetString("UserNMC", Result.UserNMC);
                                HttpContext.Session.SetString("UserID", Result.UserID);
                                HttpContext.Session.SetString("Permission", Result.Permission);
                            }
                            else
                            {
                                Result.ErrorMessage = "帳號密碼錯誤";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Result.ErrorMessage = "系統錯誤：" + ex.Message;
            }

            return Json(Result);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // 清空所有 session
            return RedirectToAction("Index", "Login");
        }
    }
}
