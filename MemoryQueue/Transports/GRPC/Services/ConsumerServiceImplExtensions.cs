using Grpc.Core;

namespace MemoryQueue.Transports.GRPC.Services
{
    public static class ConsumerServiceImplExtensions
    {
        public static ServerServiceDefinition GetServerServiceDefinition(this ConsumerServiceImpl impl) =>
            ConsumerService.BindService(impl);
    }
}
