using Microsoft.AspNetCore.Identity;
using OpenIddict.Server.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore.Authorization;

namespace BookTracker.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IOpenIddictTokenManager tokenManager
    ) : ControllerBase
{

    /// <summary>
    /// Register a new user.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return BadRequest("Email is already registered.");

        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("User registered successfully!");
    }

    /// <summary>
    /// Authenticate user and return an OpenID Connect token.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token([FromForm] OpenIddictRequest request)
    {
        if (request.IsPasswordGrantType())  // Grant type: password
        {
            var user = await userManager.FindByEmailAsync(request.Username!);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password!))
                return Unauthorized("Invalid credentials.");

            var principal = await CreatePrincipal(user);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest("Unsupported grant type.");
    }

    /// <summary>
    /// Logs the user out by revoking their access token.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Retrieve the token from the Authorization header
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token))
            return BadRequest("Invalid request.");

        // Find the token entry using the reference ID
        var tokenEntry = await tokenManager.FindByReferenceIdAsync(token);
        if (tokenEntry is null)
            return BadRequest("Invalid token.");

        // Revoke the token (this prevents it from being used again)
        var result = await tokenManager.TryRevokeAsync(tokenEntry);
        if (!result)
            return BadRequest("Could not revoke token.");

        return Ok("Logged out successfully.");
    }

    private async Task<ClaimsPrincipal> CreatePrincipal(IdentityUser user)
    {
        var principal = await signInManager.CreateUserPrincipalAsync(user);

        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim(Claims.Subject, user.Id));
        identity.AddClaim(new Claim(Claims.Email, user.Email!));

        identity.SetScopes(Scopes.Email, Scopes.Profile, "api");
        return principal;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
