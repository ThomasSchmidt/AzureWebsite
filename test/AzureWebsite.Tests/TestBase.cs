using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Website.Tests
{
    public class TestBase
    {
        public TestBase()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<TestBase>();
            Configuration = builder.Build();
        }

        protected IConfiguration Configuration { get; set; }
    }
}
