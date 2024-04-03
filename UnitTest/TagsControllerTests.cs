using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace TagsAPI.UnitTest
{
    public class TagsControllerTests
    {
        private readonly ILogger<TagsController> _logger;
        private readonly AppDbContext _context;

        public TagsControllerTests()
        {
            _logger = Mock.Of<ILogger<TagsController>>();
            _context = Mock.Of<AppDbContext>();
        }

        [Fact]
        public async Task FetchTags_Returns_OkResult()
        {
            // Arrange
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(new TagsResponse { Items = new List<Tag> { new Tag { Name = "test", Count = 10 } } })) })));

            var controller = new TagsController(_logger, _context, mockHttpClientFactory.Object);

            // Act
            var result = await controller.FetchTags();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Tags fetched successfully.", okResult.Value);
        }

        // Dodaj inne testy jednostkowe dla innych metod kontrolera...
    }

    // Klasa pomocnicza do symulowania odpowiedzi HTTP
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _responseMessage;

        public MockHttpMessageHandler(HttpResponseMessage responseMessage)
        {
            _responseMessage = responseMessage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return await Task.FromResult(_responseMessage);
        }
    }
}