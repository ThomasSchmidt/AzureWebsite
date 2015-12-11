using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AzureWebsite.Domain
{
	public class ExceptionFilterLogger : HandleErrorAttribute
	{
		public override void OnException(ExceptionContext filterContext)
		{
			if (filterContext.Exception != null)
			{
				MvcApplication.TelemetryClient.TrackException(filterContext.Exception);
			}
			base.OnException(filterContext);
		}
	}
}