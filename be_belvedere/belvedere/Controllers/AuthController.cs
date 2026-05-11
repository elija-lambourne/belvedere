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
    ///     Initiates the OIDC login flow via Keycloak.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful login (default: "/").</param>
    /// <returns>A redirect to the Keycloak login endpoint.</returns>
    /// <remarks>
    ///     This endpoint initiates the OpenID Connect code flow, redirecting the user to Keycloak for authentication.
    ///     After successful authentication, the user is redirected back to the application with an authorization code,
    ///     which is automatically exchanged for tokens by the ASP.NET Core middleware.
    ///     
    ///     The tokens are stored in HttpOnly session cookies that are automatically included in subsequent requests.
    ///     No tokens are exposed to the browser or JavaScript.
    /// </remarks>
    /// <response code="302">Redirects to Keycloak login endpoint.</response>
    [HttpGet("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        var props = new AuthenticationProperties { RedirectUri = returnUrl };
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    ///     Initiates the OIDC logout flow via Keycloak.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful logout (default: "/").</param>
    /// <returns>A redirect that clears session cookies and directs to Keycloak logout.</returns>
    /// <remarks>
    ///     This endpoint initiates the OpenID Connect logout flow. It:
    ///     1. Clears the session cookie containing the access token
    ///     2. Directs the user to Keycloak's logout endpoint to end the Keycloak session
    ///     3. Redirects back to the application after logout completes
    ///     
    ///     Requires user to be authenticated.
    /// </remarks>
    /// <response code="302">Redirects to logout flow.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout([FromQuery] string? returnUrl = "/")
    {
        var props = new AuthenticationProperties { RedirectUri = returnUrl };
        return SignOut(props, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    ///     Returns the currently authenticated user's information.
    /// </summary>
    /// <returns>A JSON object containing the user's profile information.</returns>
    /// <remarks>
    ///     <para>
    ///         This endpoint requires user authentication. The BFF uses this endpoint to:
    ///         1. Bootstrap the frontend's authentication state on app initialization
    ///         2. Determine if a user is logged in (401 response indicates logged out)
    ///         3. Refresh user information without the need for token refresh logic on the frontend
    ///     </para>
    ///     <para>
    ///         The endpoint extracts claims from the access token managed by the ASP.NET Core session:
    ///         - User ID (from "sub" claim or Name Identifier claim)
    ///         - Display Name (from "preferred_username" or Name claim)
    ///         - Email (from Email claim)
    ///         - Roles (from all Role claims)
    ///     </para>
    /// </remarks>
    /// <response code="200">Returns the authenticated user's information successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    ///     Provides the CSRF token to the frontend.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The frontend should call this endpoint on initialization and write the response token into the 
    ///         'X-XSRF-TOKEN' header for all state-changing requests (POST, PUT, DELETE, PATCH).
    ///     </para>
    ///     <para>
    ///         This endpoint:
    ///         1. Generates a new CSRF token (if one doesn't exist)
    ///         2. Stores the token in a non-HttpOnly cookie (XSRF-TOKEN) so JavaScript can read it
    ///         3. Also stores it server-side for validation
    ///         
    ///         Axios (used in the frontend) automatically reads the XSRF-TOKEN cookie and includes it in requests.
    ///         The server validates this token for all non-safe HTTP methods (POST, PUT, DELETE, PATCH).
    ///     </para>
    /// </remarks>
    /// <response code="204">CSRF token has been set in cookies.</response>
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
