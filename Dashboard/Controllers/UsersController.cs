using Microsoft.AspNetCore.Mvc;

namespace UsersMgmt.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}