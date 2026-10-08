using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace DoorCEGenerator.WebApi.Controllers;

public abstract class AuthControllerBase : ControllerBase
{
    protected string? GetCurrentUserId(bool needsAuthentication = true)
    {
        if (null != User.Identity) {
            if (!User.Identity.IsAuthenticated) {
                var token = GetToken();
                bool authorisationNotRequested = String.IsNullOrEmpty(token);
                if (!needsAuthentication && authorisationNotRequested) return null;
                bool isTokenExpired = !authorisationNotRequested && IsTokenExpired(token!);
                throw new AuthenticationException(isTokenExpired ? "User token has expired" : "User is not authenticated");
            }
            if (!string.IsNullOrEmpty(User.Identity.Name)) return User.Identity.Name;
        }
        throw new AuthenticationException("Unexpected error in user authentication");
    }
    
    private bool IsTokenExpired(string token)
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var exp = jwtToken.ValidTo;
        return exp < DateTime.UtcNow;
    }

    private string? GetToken()
    {
        if (Request.Headers.ContainsKey("Authorization")) {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
                return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }
}