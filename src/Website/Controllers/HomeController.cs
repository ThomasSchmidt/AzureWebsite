using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AzureWebsite.Models;
using Microsoft.Extensions.Options;
using Website.Infrastructure;
using Website.Models;

namespace AzureWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly IOptionsSnapshot<Settings> _settings;

        public HomeController(IOptionsSnapshot<Settings> settings)
        {
            _settings = settings;
        }

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
}
