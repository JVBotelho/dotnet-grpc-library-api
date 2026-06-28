using FluentAssertions;
using Grpc.Core;
using LibrarySystem.Grpc.Interceptors;
using LibrarySystem.UnitTests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace LibrarySystem.UnitTests.Grpc;

public class ApiKeyInterceptorTests
{
    private readonly Mock<IConfiguration> _configMock;

    public ApiKeyInterceptorTests()
    {
        _configMock = new Mock<IConfiguration>();
    }

    [Fact]
    public void Constructor_WhenKeyMissing_ShouldThrowInvalidOperationException()
    {
        _configMock.Setup(c => c["Security:GrpcApiKey"]).Returns((string?)null);

        Action act = () => new ApiKeyInterceptor(_configMock.Object);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task UnaryServerHandler_WithValidKey_ShouldInvokeContinuation()
    {
        _configMock.Setup(c => c["Security:GrpcApiKey"]).Returns("valid-key");
        var sut = new ApiKeyInterceptor(_configMock.Object);
        var headers = new Metadata { { "x-api-key", "valid-key" } };
        var context = TestServerCallContext.Create(requestHeaders: headers);

        bool continuationInvoked = false;
        Task<string> Continuation(string req, ServerCallContext ctx)
        {
            continuationInvoked = true;
            return Task.FromResult("response");
        }

        var response = await sut.UnaryServerHandler("request", context, Continuation);

        response.Should().Be("response");
        continuationInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task UnaryServerHandler_WithInvalidKey_ShouldThrowRpcException()
    {
        _configMock.Setup(c => c["Security:GrpcApiKey"]).Returns("valid-key");
        var sut = new ApiKeyInterceptor(_configMock.Object);
        var headers = new Metadata { { "x-api-key", "invalid-key" } };
        var context = TestServerCallContext.Create(requestHeaders: headers);

        Task<string> Continuation(string req, ServerCallContext ctx) => Task.FromResult("response");

        var act = async () => await sut.UnaryServerHandler("request", context, Continuation);

        await act.Should().ThrowAsync<RpcException>().Where(ex => ex.StatusCode == StatusCode.Unauthenticated);
    }

    [Fact]
    public async Task ServerStreamingServerHandler_WithValidKey_ShouldInvokeContinuation()
    {
        _configMock.Setup(c => c["Security:GrpcApiKey"]).Returns("valid-key");
        var sut = new ApiKeyInterceptor(_configMock.Object);
        var headers = new Metadata { { "x-api-key", "valid-key" } };
        var context = TestServerCallContext.Create(requestHeaders: headers);
        var responseStreamMock = new Mock<IServerStreamWriter<string>>();

        bool continuationInvoked = false;
        Task Continuation(string req, IServerStreamWriter<string> stream, ServerCallContext ctx)
        {
            continuationInvoked = true;
            return Task.CompletedTask;
        }

        await sut.ServerStreamingServerHandler("request", responseStreamMock.Object, context, Continuation);

        continuationInvoked.Should().BeTrue();
    }
}
