using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LoginMicrosoft.Controllers
{
    public class UsersController: Controller
    {
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly UserManager<IdentityUser> userManager;

        public UsersController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? message = null)
        {
            if (message is not null)
            {
                ViewData["message"] = message;
            }

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public ChallengeResult ExternalLogin(string provider, string? returnURL = null)
        {
            var redirectURL = Url.Action("RegisterExternalUser", values: new { returnURL });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectURL);
            return new ChallengeResult(provider, properties);
        }

        [AllowAnonymous]
        public async Task<IActionResult> RegisterExternalUser(string? returnURL = null,
        string? remoteError = null)
        {
            returnURL = returnURL ?? Url.Content("~/");
            var message = "";

            if (remoteError != null)
            {
                message = $"Error from external provider: {remoteError}";
                return RedirectToAction("login", routeValues: new { message });
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                message = "Error loading external login information.";
                return RedirectToAction("login", routeValues: new { message });
            }

            var externalLoginResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            // The account already exists
            if (externalLoginResult.Succeeded)
            {
                return LocalRedirect(returnURL);
            }

            string email = "";

            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                email = info.Principal.FindFirstValue(ClaimTypes.Email)!;
            }
            else
            {
                message = "Error while reading the email from the provider.";
                return RedirectToAction("login", routeValues: new { message });
            }

            var usuario = new IdentityUser() { Email = email, UserName = email };

            var createUserResult = await userManager.CreateAsync(usuario);
            if (!createUserResult.Succeeded)
            {
                message = createUserResult.Errors.First().Description;
                return RedirectToAction("login", routeValues: new { message });
            }

            var addLoginResult = await userManager.AddLoginAsync(usuario, info);

            if (addLoginResult.Succeeded)
            {
                await signInManager.SignInAsync(usuario, isPersistent: false, info.LoginProvider);
                return LocalRedirect(returnURL);
            }

            message = "There was an error while logging you in.";
            return RedirectToAction("login", routeValues: new { message });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction("Index", "Home");
        }



    }
}
