using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Authentication;

namespace MyApp.WebApp.Auth;

/// <summary>
/// Service qui stocke le token d'accès pour le circuit Blazor actuel
/// </summary>
public class TokenProvider
{
    private string? _accessToken;
    
    public string? AccessToken
    {
        get => _accessToken;
        set => _accessToken = value;
    }
    
    public bool HasToken => !string.IsNullOrEmpty(_accessToken);
}

/// <summary>
/// Circuit handler qui initialise le TokenProvider avec le token d'accès
/// lors de la connexion du circuit Blazor
/// </summary>
public class TokenCircuitHandler : CircuitHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenProvider _tokenProvider;
    
    public TokenCircuitHandler(IHttpContextAccessor httpContextAccessor, TokenProvider tokenProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenProvider = tokenProvider;
    }
    
    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Récupérer le token depuis le HttpContext (disponible uniquement lors de la connexion initiale)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                _tokenProvider.AccessToken = accessToken;
                Console.WriteLine($"[TokenCircuitHandler] Token récupéré et stocké pour le circuit");
            }
            else
            {
                Console.WriteLine($"[TokenCircuitHandler] Aucun token disponible dans le HttpContext");
            }
        }
        else
        {
            Console.WriteLine($"[TokenCircuitHandler] HttpContext non disponible");
        }
    }
}

