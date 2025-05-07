using Microsoft.AspNetCore.Mvc;

namespace ochweb.Controllers
{
	public class HomeController : BaseController
    {
		public IActionResult Index()
		{
			return View();
		}
	}
}
