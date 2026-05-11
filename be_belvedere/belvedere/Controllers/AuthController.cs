using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace belvedere.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IAntiforgery antiforgery) : ControllerBase
{
    /// <summary>
    /// Initiates the OIDC login flow.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful login.</param>
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        var props = new AuthenticationProperties { RedirectUri = returnUrl };
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Initiates the OIDC logout flow.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after logout.</param>
    [HttpGet("logout")]
    [Authorize]
    public IActionResult Logout([FromQuery] string? returnUrl = "/")
    {
        var props = new AuthenticationProperties { RedirectUri = returnUrl };
        return SignOut(props, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Returns the currently authenticated user's information.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var user = User.Identity;
        if (user is null || !user.IsAuthenticated)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value,
            Name = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("preferred_username")?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        });
    }

    /// <summary>
    /// Provides the CSRF token to the frontend.
    /// </summary>
    /// <remarks>
    /// The frontend should call this endpoint on initialization and write the response token
    /// into the 'X-XSRF-TOKEN' header for all state-changing requests (POST, PUT, DELETE).
    /// </remarks>
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        
        // Write the token to a non-HttpOnly cookie so the SPA can read it (Axios does this automatically)
        HttpContext.Response.Cookies.Append(
            "XSRF-TOKEN",
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
                Path = "/",
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict // Adjust to Lax if cross-origin redirects break
            });

        return NoContent();
    }
}
