using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ochweb.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(user))
            {
                context.Result = RedirectToAction("Index", "Login");
            }
            base.OnActionExecuting(context);
        }
    }
}
