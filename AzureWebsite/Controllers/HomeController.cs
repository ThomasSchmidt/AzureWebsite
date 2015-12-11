using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AzureWebsite.Controllers
{
	public class HomeController : Controller
	{
		private Stopwatch _w;
		public ActionResult Index()
		{
			ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

			return View();
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			_w = Stopwatch.StartNew();
			base.OnActionExecuting(filterContext);
		}

		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			_w.Stop();

			var properties = new Dictionary<string, string> { { "Controllers", "ExecutionTime" } };
			var measurements = new Dictionary<string, double> { { "ControllerExecutionTime", _w.ElapsedMilliseconds } };
			MvcApplication.TelemetryClient.TrackEvent("Controller", properties, measurements);

			base.OnActionExecuted(filterContext);
		}
	}
}
