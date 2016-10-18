using System.Web;
using System.Web.Mvc;
using AzureWebsite.Domain;

namespace AzureWebsite
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new ErrorHandler.AiHandleErrorAttribute());
			filters.Add(new ExceptionFilterLogger());
		}
	}
}