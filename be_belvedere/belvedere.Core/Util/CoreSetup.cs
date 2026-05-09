using belvedere.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace belvedere.Core.Util;

public static class CoreSetup
{
    public static void ConfigureCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        
        services.AddScoped<IRocketService, RocketService>();
    }
}
