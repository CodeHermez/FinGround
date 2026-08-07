namespace FinGround.McpServer.Api;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the typed FinGround API client plus the transport-appropriate token provider.
    /// </summary>
    public static IServiceCollection AddFinGroundApiClient(
        this IServiceCollection services, IConfiguration configuration, McpTransport transport)
    {
        services.Configure<FinGroundApiOptions>(
            configuration.GetSection(FinGroundApiOptions.SectionName));
        services.Configure<McpServerSettings>(
            configuration.GetSection(McpServerSettings.SectionName));

        services.AddSingleton(new McpTransportContext(transport));

        // Both providers are singletons on purpose: IHttpClientFactory pools handler chains for
        // ~2 minutes and resolves their dependencies outside the current request scope, so a
        // scoped provider injected into BearerTokenHandler would be a captive dependency.
        if (transport == McpTransport.Stdio)
        {
            services.AddSingleton<StdioTokenProvider>();
            services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<StdioTokenProvider>());
        }
        else
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<ITokenProvider, HttpContextTokenProvider>();
        }

        // No bearer handler on this one — StdioTokenProvider logs in through it.
        services.AddHttpClient(FinGroundApiClient.AnonymousClientName, ConfigureClient);

        services.AddTransient<BearerTokenHandler>();
        services.AddHttpClient<FinGroundApiClient>(ConfigureClient)
                .AddHttpMessageHandler<BearerTokenHandler>();

        return services;

        static void ConfigureClient(IServiceProvider sp, HttpClient client)
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<FinGroundApiOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        }
    }
}
