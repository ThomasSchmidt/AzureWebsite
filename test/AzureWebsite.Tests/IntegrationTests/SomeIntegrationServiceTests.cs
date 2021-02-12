using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Website.Tests.IntegrationTests
{
    [Trait("Category", "integrationtest")]
    public class SomeIntegrationServiceTests
    {
        [Fact]
        public async Task Website_GivenUrl_WillDownload()
        {
            using var httpClient = new HttpClient();
            var google = new Uri("https://www.google.com");
            var response = await httpClient.GetAsync(google);
            var content = await response.Content.ReadAsStringAsync();

            Assert.NotNull(content);
        }

        [Fact]
        public void Crm_GivenValidOrder_CanSendToCrm()
        {
            // var crmService = GetCrmService();

            // var dummyOrder = GetDummyOrder();
            // dummyOrder.Navn = "test-mand";

            // var crmResponse = crmService.PlaceOrder(dummyOrder);

            // Assert.True(crmResponse.OrderId > 0);

            // var crmOrder = crmService.GetOrder(crmResponse.OrderId);

            // Assert.Equal(crmOrder.Navn, "test-mand");
        }
    }
}
