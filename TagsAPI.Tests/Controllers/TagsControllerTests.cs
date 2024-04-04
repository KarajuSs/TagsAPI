using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Net;
using TagsAPI.Data;
using TagsAPI.Model;

namespace TagsAPI.Tests.Controllers
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

        [Fact]
        public async Task CalculatePercentage_Returns_OkResult()
        {
            // Arrange
            var tags = new List<Tag>
            {
                new Tag { Name = "java", Count = 10 },
                new Tag { Name = "c#", Count = 20 },
                new Tag { Name = "html", Count = 30 }
            };

            var mockDbSet = new Mock<DbSet<Tag>>();
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.Provider).Returns(tags.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.Expression).Returns(tags.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.ElementType).Returns(tags.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.GetEnumerator()).Returns(tags.AsQueryable().GetEnumerator());

            var mockDbContext = new Mock<AppDbContext>();
            mockDbContext.Setup(c => c.Tags).Returns(mockDbSet.Object);

            var controller = new TagsController(_logger, mockDbContext.Object);

            // Act
            var result = await controller.CalculatePercentage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var percentageTags = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(3, percentageTags.Count());
        }

        [Fact]
        public async Task GetTags_Returns_OkResult()
        {
            // Arrange
            var tags = new List<Tag>
            {
                new Tag { Name = "java", Count = 10 },
                new Tag { Name = "c#", Count = 20 },
                new Tag { Name = "html", Count = 30 }
            };

            var mockDbSet = new Mock<DbSet<Tag>>();
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.Provider).Returns(tags.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.Expression).Returns(tags.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.ElementType).Returns(tags.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.GetEnumerator()).Returns(tags.AsQueryable().GetEnumerator());

            var mockDbContext = new Mock<AppDbContext>();
            mockDbContext.Setup(c => c.Tags).Returns(mockDbSet.Object);

            var controller = new TagsController(_logger, mockDbContext.Object);

            // Act
            var result = await controller.GetTags();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<Tag>>(okResult.Value);
            Assert.Equal(3, returnedTags.Count());
        }
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
