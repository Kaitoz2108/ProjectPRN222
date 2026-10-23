using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using CafeManagementSystem.Models;

namespace CafeManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Cashier" => RedirectToAction("Shift", "Cashier"),
                "Barista" => RedirectToAction("KDS", "Barista"),
                "DeliveryStaff" => RedirectToAction("Index", "Delivery"),
                "Customer" => RedirectToAction("Menu", "Customer"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        return RedirectToAction("Login", "Account");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
