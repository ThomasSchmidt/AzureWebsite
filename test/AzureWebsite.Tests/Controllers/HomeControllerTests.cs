using AzureWebsite.Controllers;
using AzureWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Website.Infrastructure;
using Xunit;

namespace Website.Tests.Controllers
{
    [Trait("Category", "unittest")]
    public class HomeControllerTests
    {
        [Fact]
        public void Index_GivenValidInput_CanExecuteIndex()
        {
            var settings = GetSettings();
            var sut = new HomeController(settings);

            var actual = sut.Index();

            Assert.NotNull(actual);
            Assert.IsType<ViewResult>(actual);
        }

        [Fact]
        public void Index_GivenValidInput_CanExecutePrivacy()
        {
            var settings = GetSettings();
            var sut = new HomeController(settings);

            var actual = sut.Privacy();

            Assert.NotNull(actual);
            Assert.IsType<ViewResult>(actual);
        }

        [Fact]
        public void Index_GivenValidInput_CanExecuteError()
        {
            var settings = GetSettings();
            var sut = new HomeController(settings);

            var actual = sut.Error();

            Assert.NotNull(actual);
            Assert.IsType<ViewResult>(actual);

            var model = ((ViewResult)actual).Model as ErrorViewModel;
            Assert.NotNull(model);
            Assert.NotNull(model.RequestId);
        }

        private IOptionsSnapshot<Settings> GetSettings()
        {
            return new OptionsSettings();
        }

        public class OptionsSettings : IOptionsSnapshot<Settings>
        {
            public Settings Value => new Settings();

            public Settings Get(string name)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}