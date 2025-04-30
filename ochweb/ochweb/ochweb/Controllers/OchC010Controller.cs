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
	public class OchC010Controller : Controller
	{
		public ActionResult Index()
		{
			OchC010View NewOchC010 = new OchC010View();
			return View("Index", NewOchC010);
		}
	}
}
