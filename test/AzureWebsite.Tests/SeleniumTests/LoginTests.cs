using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Website.Tests.SeleniumTests
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/devops/pipelines/test/continuous-test-selenium?view=azure-devops
    /// </summary>
    [Trait("Category", "seleniumtest")]
    public class LoginTests : TestBase, IDisposable
    {
        private readonly IWebDriver _driver;

        public LoginTests()
        {
            // needs to use a special path when running in azure pipelines
            var chromeDriverPath = Environment.GetEnvironmentVariable("ChromeWebDriver");
            _driver = !string.IsNullOrWhiteSpace(chromeDriverPath) 
                ? new ChromeDriver(chromeDriverPath)
                : new ChromeDriver();
        }

        public void Dispose()
        {
            _driver.Dispose();
        }

        [Fact]
        public void LoginTest_GivenUserNameAndPassword_ShouldLogin()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
            wait.PollingInterval = TimeSpan.FromSeconds(1);
            
            // navigate to cancer forum
            _driver.Navigate().GoToUrl("https://www.cancerforum.dk/");

            // accept cookies
            var waitForCookieAcceptEl = WaitFor(By.CssSelector("button.coi-banner__accept"));
            var cookieAcceptEl = wait.Until(waitForCookieAcceptEl);
            cookieAcceptEl.Click();

            //navigate to login screen which is rest based, so we have to fake it
            _driver.Navigate().GoToUrl("https://www.cancerforum.dk/rest/sign-in/");

            // find username text box:
            var usernameEl = _driver.FindElement(By.Name("username"));

            // put in username
            var username = Configuration["Tests:Integration:CancerforumUsername"];
            usernameEl.SendKeys(username);

            // find password text box
            var passwordEl = _driver.FindElement(By.Name("password"));

            // put in password
            var password = Configuration["Tests:Integration:CancerforumPassword"];
            passwordEl.SendKeys("MegetLangtPassword1");

            // find login button - modal__form__submit btn btn--round btn--cta
            // body>section>form>button
            var loginButtonEl = _driver.FindElement(By.CssSelector("body>section>form>button"));

            // click login button
            loginButtonEl.Click();

            // navigate to front page again and verify
            var url = Configuration["Tests:Integration:CancerforumUrl"];
            _driver.Navigate().GoToUrl(url);

            // check if we have the login icon - user-bar__content__login__name
            var waitForVerifyEl = WaitFor(By.ClassName("user-bar__content__login__name"));
            var verifyEl = wait.Until(waitForVerifyEl);

            Assert.NotNull(verifyEl);
            Assert.Equal("ThomasSchmidtTest", verifyEl.Text);
        }

        private Func<IWebDriver, IWebElement> WaitFor(By by)
        {
            Func<IWebDriver, IWebElement> f = (driver) =>
            {
                try
                {
                    var e = driver.FindElement(by);
                    if (e.Displayed)
                    {
                        return e;
                    }
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            };
            return f;
        }
    }
}
