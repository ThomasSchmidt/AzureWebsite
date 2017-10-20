using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AzureWebsite.Infrastructure.Filters
{
    public class RequireHttpsOnAzureAttribute : RequireHttpsAttribute
    {
        private readonly IHostingEnvironment _environment;

        public RequireHttpsOnAzureAttribute(IHostingEnvironment environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _environment = environment;
        }

        public override void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (!_environment.IsDevelopment())
            {
                base.OnAuthorization(filterContext);
            }
        }

        protected override void HandleNonHttpsRequest(AuthorizationFilterContext filterContext)
        {
            if (!_environment.IsDevelopment())
            {
                base.HandleNonHttpsRequest(filterContext);
            }
        }
    }
}
