using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HelloWorld.Tests
{
    public class TestValuesController : IClassFixture<WebApplicationFactory<Program>>
    {
        public TestValuesController(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        private readonly WebApplicationFactory<Program> _factory;

        [Fact]
        public async Task GetValuesReturnsDefaultValues()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            using var response = await client.GetAsync("/api/values");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(new[] { "value1", "value2" }, await response.Content.ReadFromJsonAsync<string[]>());
        }

        [Fact]
        public async Task GetValuesUsesInjectedService()
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IValuesService, TestValuesService>();
                });
            });
            using var client = factory.CreateClient();

            var values = await client.GetFromJsonAsync<string[]>("/api/values");
            Assert.Equal(new[] { "injected-value", "another-value" }, values);
        }
    }

    public class TestValuesService : IValuesService
    {
        public IEnumerable<string> GetValues()
        {
            return new List<string> { "injected-value", "another-value" };
        }
    }
}
