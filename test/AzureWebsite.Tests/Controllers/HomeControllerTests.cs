using AzureWebsite.Controllers;
using AzureWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Website.Tests.Controllers
{
    [Trait("Category", "unittest")]
    public class HomeControllerTests
    {
        [Fact]
        public void Index_GivenValidInput_CanExecuteIndex()
        {
            var sut = new HomeController();

            var actual = sut.Index();

            Assert.NotNull(actual);
            Assert.IsType<ViewResult>(actual);
        }

        [Fact]
        public void Index_GivenValidInput_CanExecutePrivacy()
        {
            var sut = new HomeController();

            var actual = sut.Privacy();

            Assert.NotNull(actual);
            Assert.IsType<ViewResult>(actual);
        }

        [Fact]
        public void Index_GivenValidInput_CanExecuteError()
        {
            var sut = new HomeController();

            var actual = sut.Error();

            Assert.NotNull(actual);
            Assert.IsType<ViewResult>(actual);

            var model = ((ViewResult)actual).Model as ErrorViewModel;
            Assert.NotNull(model);
            Assert.NotNull(model.RequestId);
        }
    }
}