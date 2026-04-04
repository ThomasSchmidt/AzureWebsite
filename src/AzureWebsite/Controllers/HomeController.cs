using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AzureWebsite.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.OutputCaching;

namespace AzureWebsite.Controllers;

public class HomeController : Controller
{
    private readonly IOptions<WebsiteSettings> _settings;

    public HomeController(IOptions<WebsiteSettings> settings)
    {
        _settings = settings;
    }

	[OutputCache(Duration = 6000)]
    public IActionResult Index()
    {
        var model = new HomeViewModel();
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id 
                ?? HttpContext?.TraceIdentifier
                ?? Guid.NewGuid().ToString()
        });
    }
}
