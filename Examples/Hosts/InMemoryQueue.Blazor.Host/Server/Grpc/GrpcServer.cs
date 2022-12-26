
using Grpc.Core;
using MemoryQueue.GRPC.Transports.GRPC.Services;
using GrpcSocketServer = Grpc.Core.Server;

namespace InMemoryQueue.Blazor.Host.Grpc
{
    public class GrpcServer
    {
        private readonly ILogger<GrpcServer> _logger;
        private readonly GrpcSocketServer _server;

        public GrpcServer(IServiceProvider sp, ILogger<GrpcServer> logger)
        {
            _logger = logger;
            _server = new GrpcSocketServer()
            {
                Services = { sp.GetRequiredService<ConsumerServiceImpl>().GetServerServiceDefinition() },
                Ports = { new ServerPort("0.0.0.0", 1111, ServerCredentials.Insecure) }
            };
            logger.LogInformation("GrpcServer Configured at: 0.0.0.0:1111");
        }

        public void Start()
        {
            _server.Start();
            _logger.LogInformation("GrpcServer Started at: 0.0.0.0:1111");
        }

    }
}
