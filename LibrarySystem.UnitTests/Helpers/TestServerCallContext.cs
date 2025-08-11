using Grpc.Core;
using Moq;

namespace LibrarySystem.UnitTests.Helpers;

public static class TestServerCallContext
{
    public static ServerCallContext Create()
    {
        return Mock.Of<ServerCallContext>();
    }
}