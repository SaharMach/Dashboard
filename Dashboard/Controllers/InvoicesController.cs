using Microsoft.AspNetCore.Mvc;

public class InvoicesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
