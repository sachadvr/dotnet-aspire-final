using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace MyApp.WebApp.Auth;

internal sealed class CookieOidcRefresher(IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor)
{
    private readonly OpenIdConnectProtocolValidator oidcTokenValidator = new()
    {
        RequireNonce = false,
    };

    public async Task ValidateOrRefreshCookieAsync(CookieValidatePrincipalContext validateContext, string oidcScheme)
    {
        // D'abord, extraire les rôles depuis le token d'accès si nécessaire
        ExtractRolesFromAccessToken(validateContext);

        // Ensuite, vérifier si le token doit être rafraîchi
        var accessTokenExpirationText = validateContext.Properties.GetTokenValue("expires_at");
        if (!DateTimeOffset.TryParse(accessTokenExpirationText, out var accessTokenExpiration))
        {
            return;
        }

        var oidcOptions = oidcOptionsMonitor.Get(oidcScheme);
        var now = oidcOptions.TimeProvider!.GetUtcNow();
        if (now + TimeSpan.FromMinutes(5) < accessTokenExpiration)
        {
            return;
        }

        var oidcConfiguration = await oidcOptions.ConfigurationManager!.GetConfigurationAsync(validateContext.HttpContext.RequestAborted);
        var tokenEndpoint = oidcConfiguration.TokenEndpoint ?? throw new InvalidOperationException("Cannot refresh cookie. TokenEndpoint missing!");

        using var refreshResponse = await oidcOptions.Backchannel.PostAsync(tokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string?>()
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = oidcOptions.ClientId,
                ["client_secret"] = oidcOptions.ClientSecret,
                ["scope"] = string.Join(" ", oidcOptions.Scope),
                ["refresh_token"] = validateContext.Properties.GetTokenValue("refresh_token"),
            }));

        if (!refreshResponse.IsSuccessStatusCode)
        {
            validateContext.RejectPrincipal();
            return;
        }

        var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
        var message = new OpenIdConnectMessage(refreshJson);

        var validationParameters = oidcOptions.TokenValidationParameters.Clone();
        validationParameters.ValidIssuer = oidcConfiguration.Issuer;
        validationParameters.IssuerSigningKeys = oidcConfiguration.SigningKeys;

        var validationResult = await oidcOptions.TokenHandler.ValidateTokenAsync(message.IdToken, validationParameters);

        if (!validationResult.IsValid)
        {
            validateContext.RejectPrincipal();
            return;
        }

        validateContext.ShouldRenew = true;
        
        // Extraire les rôles depuis le nouveau token d'accès et les ajouter au principal
        var newIdentity = validationResult.ClaimsIdentity;
        ExtractRolesFromAccessToken(message.AccessToken, newIdentity);
        
        validateContext.ReplacePrincipal(new System.Security.Claims.ClaimsPrincipal(newIdentity));

        var expiresIn = int.Parse(message.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var expiresAt = now + TimeSpan.FromSeconds(expiresIn);
        validateContext.Properties.StoreTokens([
            new() { Name = "access_token", Value = message.AccessToken },
            new() { Name = "id_token", Value = message.IdToken },
            new() { Name = "refresh_token", Value = message.RefreshToken },
            new() { Name = "token_type", Value = message.TokenType },
            new() { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) },
        ]);
    }

    private void ExtractRolesFromAccessToken(CookieValidatePrincipalContext validateContext)
    {
        if (validateContext.Principal?.Identity is not System.Security.Claims.ClaimsIdentity identity)
        {
            return;
        }

        // Vérifier si les rôles sont déjà présents
        var existingRoles = identity.FindAll("roles").ToList();
        if (existingRoles.Any())
        {
            return; // Les rôles sont déjà présents
        }

        // Extraire depuis le token d'accès stocké
        var accessToken = validateContext.Properties.GetTokenValue("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        ExtractRolesFromAccessToken(accessToken, identity);

        if (identity.FindAll("roles").Any())
        {
            validateContext.ReplacePrincipal(new System.Security.Claims.ClaimsPrincipal(identity));
            validateContext.ShouldRenew = true;
        }
    }

    private void ExtractRolesFromAccessToken(string accessToken, System.Security.Claims.ClaimsIdentity identity)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var accessTokenJwt = handler.ReadJwtToken(accessToken);
            var realmAccessClaim = accessTokenJwt.Claims.FirstOrDefault(c => c.Type == "realm_access");

            if (realmAccessClaim != null)
            {
                var realmAccess = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(realmAccessClaim.Value);
                if (realmAccess.TryGetProperty("roles", out var rolesArray))
                {
                    var rolesAdded = new List<string>();
                    foreach (var role in rolesArray.EnumerateArray())
                    {
                        var roleValue = role.GetString();
                        if (!string.IsNullOrEmpty(roleValue) && !identity.HasClaim("roles", roleValue))
                        {
                            identity.AddClaim(new System.Security.Claims.Claim("roles", roleValue));
                            rolesAdded.Add(roleValue);
                        }
                    }
                    if (rolesAdded.Any())
                    {
                        Console.WriteLine($"✅ [CookieOidcRefresher] Rôles extraits depuis le token d'accès: {string.Join(", ", rolesAdded)}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [CookieOidcRefresher] Erreur lors de l'extraction des rôles: {ex.Message}");
        }
    }
}

