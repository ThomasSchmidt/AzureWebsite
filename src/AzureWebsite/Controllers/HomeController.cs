using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AzureWebsite.Models;
using Microsoft.Extensions.Options;
using Website.Models;
using Microsoft.AspNetCore.OutputCaching;

namespace AzureWebsite.Controllers;

public class HomeController : Controller
{
    private readonly IOptions<Settings> _settings;

    public HomeController(IOptions<Settings> settings)
    {
        _settings = settings;
    }

	[OutputCache(Duration = 6000)]
    public IActionResult Index()
    {
        var model = new HomeViewModel
        {
            ShowThis = _settings.Value.Showthis,
        };

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
