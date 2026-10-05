using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Api.Controllers;

[Authorize]
public sealed class HomeController(
    UserManager<IdentityUser> userManager,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["CanBootstrapAdmin"] = environment.IsDevelopment()
            && !User.IsInRole("Admin")
            && (await userManager.GetUsersInRoleAsync("Admin")).Count == 0;

        return View();
    }
}