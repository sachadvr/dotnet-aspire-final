using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace MyApp.WebApp.Auth;

internal static class CookieOidcServiceCollectionExtensions
{
    public static IServiceCollection ConfigureCookieOidc(this IServiceCollection services, string cookieScheme, string oidcScheme)
    {
        services.AddSingleton<CookieOidcRefresher>();
        services.AddOptions<CookieAuthenticationOptions>(cookieScheme).Configure<CookieOidcRefresher>((cookieOptions, refresher) =>
        {
            cookieOptions.Events.OnValidatePrincipal = context => refresher.ValidateOrRefreshCookieAsync(context, oidcScheme);
        });
        services.AddOptions<OpenIdConnectOptions>(oidcScheme).Configure(oidcOptions =>
        {
            oidcOptions.Scope.Add("offline_access");
            oidcOptions.SaveTokens = true;
        });
        return services;
    }
}

