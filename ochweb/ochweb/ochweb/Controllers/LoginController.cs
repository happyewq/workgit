using Microsoft.AspNetCore.Mvc;
using ochweb.Models;
using ochweb.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;

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
                using (SqlConnection conn = new SqlConnection(connstring))
                {
                    conn.Open();

                    string sql = "SELECT * FROM OCHUSER WHERE UserID = @UserID AND Password = @Password";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", UserID);
                        cmd.Parameters.AddWithValue("@Password", Password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Result.UserNMC = reader["UserNMC"].ToString();
                                Result.UserID = reader["UserID"].ToString();
                            }
                            else
                            {
                                Result.ErrorMessage = "登入失敗";
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
    }
}
