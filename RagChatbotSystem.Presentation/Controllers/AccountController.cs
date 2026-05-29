using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbotSystem.Business.Interfaces;
using RagChatbotSystem.Presentation.Authorization;

namespace RagChatbotSystem.Presentation.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AccountController : Controller
    {
        private const string GoogleScheme = "Google";
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, GoogleScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                return Unauthorized("Google authentication failed.");
            }

            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return BadRequest("Google account does not provide an email.");
            }

            var user = await _accountService.FindOrCreateGoogleUserAsync(email, fullName ?? email);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Policy = AuthPolicies.AdminOrUser)]
        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Policy = AuthPolicies.AdminOrUser)]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized("Invalid user session.");
            }

            var user = await _accountService.GetUserByIdAsync(userId);
            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Unauthorized("User does not exist.");
            }

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Role,
                user.CreatedAt
            });
        }

        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Access denied.");
        }
    }
}
