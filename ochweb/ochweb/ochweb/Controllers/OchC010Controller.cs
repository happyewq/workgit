﻿using Microsoft.AspNetCore.Mvc;
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
	public class OchC010Controller : BaseController
    {
		public ActionResult Index()
		{
			OchC010View NewOchC010 = new OchC010View();
			return View("Index", NewOchC010);
		}   
        public ActionResult Insert()
        {
            OchC010View NewOchC010 = new OchC010View();
            return View("Insert", NewOchC010);
        }
        public ActionResult Edit(OchC010View NewOchC010)
        {
            return View("Edit", NewOchC010);
        }
    }
}
