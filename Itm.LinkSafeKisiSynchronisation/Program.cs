using Itm.LinkSafeKisiSynchronisation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration(builder =>
    {
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((builder, services) =>
    {
        services.AddLogging();
        services.AddOptions();
        services.AddOptions<LinkSafeConfig>().Bind(builder.Configuration.GetSection(nameof(LinkSafeConfig)));
        services.AddOptions<KisisConfig>().Bind(builder.Configuration.GetSection(nameof(KisisConfig)));
        services.AddScoped<ErrorService>();
        services.AddScoped<Kisis>();
        services.AddScoped<LinkSafe>();
    })
    
    .Build();

host.Run();