using AzureWebsite.Controllers;
using AzureWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GivenValidInput_CanExecuteIndex()
        {
            var sut = new HomeController();

            var actual = sut.Index();

            Assert.IsNotNull(actual);
            Assert.That(actual, Is.TypeOf<ViewResult>());
        }

        [Test]
        public void GivenValidInput_CanExecutePrivacy()
        {
            var sut = new HomeController();

            var actual = sut.Privacy();

            Assert.IsNotNull(actual);
            Assert.That(actual, Is.TypeOf<ViewResult>());
        }

        [Test]
        public void GivenValidInput_CanExecuteError()
        {
            var sut = new HomeController();

            var actual = sut.Error();

            Assert.IsNotNull(actual);
            Assert.That(actual, Is.TypeOf<ViewResult>());

            var model = ((ViewResult)actual).Model as ErrorViewModel;
            Assert.IsNotNull(model);
            Assert.IsNotNull(model.RequestId);
        }
    }
}