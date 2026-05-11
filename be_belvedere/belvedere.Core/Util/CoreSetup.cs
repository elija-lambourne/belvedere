using belvedere.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace belvedere.Core.Util;

public static class CoreSetup
{
    public static void ConfigureCore(this IServiceCollection services, IConfigurationManager configurationManager)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        
        services.Configure<StorageSettings>(configurationManager.GetSection(StorageSettings.SectionKey));

        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IAlbumService, AlbumService>();
        services.AddScoped<IShareService, ShareService>();
        services.AddScoped<IStorageService, S3StorageService>();

    }
}
