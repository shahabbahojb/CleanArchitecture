using CleanArchitecture.Application.Common.Behaviours;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.TodoItems.Commands.CreateTodoItem;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace CleanArchitecture.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateTodoItemCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    private const string MessageTemplate = "CleanArchitecture Request: {Name} {@UserId} {@UserName} {@Request}";

    private static bool MatchLogState(object? state, string requestName, string userId, string? userName, CreateTodoItemCommand request)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object?>> values)
        {
            var dict = values.ToDictionary(kv => kv.Key, kv => kv.Value);
            return
                dict.TryGetValue("{OriginalFormat}", out var format) && Equals(format, MessageTemplate) &&
                dict.TryGetValue("Name", out var name) && Equals(name, requestName) &&
                dict.TryGetValue("UserId", out var id) && Equals(id, userId) &&
                dict.TryGetValue("UserName", out var uName) && Equals(uName, userName) &&
                dict.TryGetValue("Request", out var req) && Equals(req, request);
        }

        return false;
    }

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateTodoItemCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        var userId = Guid.NewGuid().ToString();
        _user.Setup(x => x.Id).Returns(userId);
        _identityService.Setup(i => i.GetUserNameAsync(userId)).ReturnsAsync(string.Empty);

        var request = new CreateTodoItemCommand { ListId = 1, Title = "title" };
        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(request, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Once);
        _logger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => MatchLogState(v, nameof(CreateTodoItemCommand), userId, string.Empty, request)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand>(_logger.Object, _user.Object, _identityService.Object);

        var request = new CreateTodoItemCommand { ListId = 1, Title = "title" };
        await requestLogger.Process(request, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Never);
        _logger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => MatchLogState(v, nameof(CreateTodoItemCommand), string.Empty, string.Empty, request)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
