using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

namespace belvedere.Controllers;

/// <summary>
/// API controller handling the Backend-For-Frontend (BFF) authentication flows.
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Initiates the OpenID Connect login flow.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after successful login.</param>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Initiates the OpenID Connect logout flow.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after successful logout.</param>
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout([FromQuery] string returnUrl = "/")
    {
        // Sign out of both the local cookie and the upstream OIDC provider
        return SignOut(new AuthenticationProperties { RedirectUri = returnUrl }, 
            CookieAuthenticationDefaults.AuthenticationScheme, 
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Returns the currently authenticated user's profile and claims.
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var claims = User.Claims.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToList());
        
        return Ok(new 
        { 
            IsAuthenticated = true, 
            Name = User.Identity.Name,
            UserId = User.FindFirst("sub")?.Value,
            Claims = claims 
        });
    }

    /// <summary>
    /// Provides an Anti-Forgery (CSRF) token to the frontend.
    /// </summary>
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        
        // Append the token as a cookie that JavaScript can read
        HttpContext.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false, // Critical: JS must be able to read it to send it in the header
            Secure = true,    // Enforce HTTPS
            SameSite = SameSiteMode.Strict, // Strictly limit to the same site
            Path = "/"
        });

        return NoContent();
    }
}
