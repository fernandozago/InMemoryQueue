using Grpc.Core;
using MemoryQueue.GRPC.Transports.GRPC.Services;

namespace GrpcService2.GrpcImpl
{
    public class GrpcServer
    {
        private readonly ILogger<GrpcServer> _logger;
        private readonly Server _server;

        public GrpcServer(IServiceProvider sp, ILogger<GrpcServer> logger)
        {
            _logger = logger;
            _server = new Server()
            {
                Services = { sp.GetRequiredService<ConsumerServiceImpl>().GetServiceDefinition() },
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
