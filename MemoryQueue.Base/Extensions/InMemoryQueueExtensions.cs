using Microsoft.Extensions.DependencyInjection;

namespace MemoryQueue.Base.Extensions;

public static class InMemoryQueueExtensions
{
    public static IServiceCollection AddInMemoryQueue(this IServiceCollection provider) =>
        provider
            .AddSingleton<IInMemoryQueueManager, InMemoryQueueManager>();
}
