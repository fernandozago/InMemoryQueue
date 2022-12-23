using Grpc.Core;
using MemoryQueue.Transports.GRPC;

namespace MemoryQueue.Models.GRPC.Services
{
    public static class ConsumerServiceImplExtensions
    {
        public static ServerServiceDefinition GetServerServiceDefinition(this ConsumerServiceImpl impl) =>
            ConsumerService.BindService(impl);
    }
}
