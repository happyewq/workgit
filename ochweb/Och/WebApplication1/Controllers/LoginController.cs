using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
	public class LoginController : Controller
	{
		public ActionResult Index()
		{
			// 查詢
			LoginView NewLoginViewView = new LoginView();
			//
			return View("Index", NewLoginViewView);
		}

		[HttpPost]
		public JsonResult UserLogin(string UserID, string Password)
		{
			// 這裡可以放你的登入驗證邏輯，例如資料庫比對
			if (UserID == "admin" && Password == "123456")
			{
				return Json(new { success = true, message = "登入成功" });
			}
			else
			{
				return Json(new { success = false, message = "帳號或密碼錯誤" });
			}
		}
	}
}
