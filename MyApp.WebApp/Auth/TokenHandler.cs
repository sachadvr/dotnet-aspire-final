using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;

namespace MyApp.WebApp.Auth;

public class TokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    
    public TokenHandler(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? accessToken = null;
        
        if (_httpContextAccessor.HttpContext != null)
        {
            accessToken = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
            Console.WriteLine($"[TokenHandler] Token depuis HttpContext: {(accessToken != null ? "Trouvé" : "Null")}");
        }
        
        if (string.IsNullOrEmpty(accessToken))
        {
            try
            {
                var tokenProvider = _serviceProvider.GetService<TokenProvider>();
                if (tokenProvider != null && tokenProvider.HasToken)
                {
                    accessToken = tokenProvider.AccessToken;
                    Console.WriteLine($"[TokenHandler] Token depuis TokenProvider: Trouvé");
                }
                else
                {
                    Console.WriteLine($"[TokenHandler] TokenProvider: {(tokenProvider == null ? "Null" : "Pas de token")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TokenHandler] Erreur lors de la récupération du TokenProvider: {ex.Message}");
            }
        }
        
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            Console.WriteLine($"[TokenHandler] ✅ Token ajouté au header pour {request.RequestUri}");
            
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;
                
                if (jsonToken != null)
                {
                    Console.WriteLine($"=== Contenu du Token JWT ===");
                    Console.WriteLine($"Issuer: {jsonToken.Issuer}");
                    Console.WriteLine($"Audience: {string.Join(", ", jsonToken.Audiences)}");
                    Console.WriteLine($"Expiration: {jsonToken.ValidTo:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Claims:");
                    foreach (var claim in jsonToken.Claims)
                    {
                        Console.WriteLine($"  - {claim.Type}: {claim.Value}");
                    }
                    Console.WriteLine($"===========================");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du décodage du token: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[TokenHandler] ❌ Pas de token disponible pour {request.RequestUri}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

