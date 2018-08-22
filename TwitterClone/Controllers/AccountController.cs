using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TwitterClone.Models;
using TwitterClone.EF;
using TwitterClone.Helpers;

namespace TwitterClone.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private TwitterCloneEntities _twitterCloneEntity = new TwitterCloneEntities();
        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, shouldLockout: false);

            var person = _twitterCloneEntity.People.Where(x => x.user_id == model.UserName && x.active).Select(x => x).FirstOrDefault();

            if (result != SignInStatus.Failure && person != null && SimpleHash.VerifyHash(model.Password, "MD5", person.password))
            {
                result = SignInStatus.Success;
            }
            else
                result = SignInStatus.Failure;

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };

                var isExistingUser = await UserManager.FindByNameAsync(model.UserName);

                if (isExistingUser != null)
                {
                    ModelState.AddModelError("userexist", string.Format("Username {0} already exist in Twitter Clone!", model.UserName));
                    return View(model);
                }                
                else
                {
                    var result = await UserManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                        if (_twitterCloneEntity.People.Any(x => x.user_id == model.UserName && !x.active))
                        {
                            Person person = _twitterCloneEntity.People.Where(x => x.user_id == model.UserName).Select(x => x).FirstOrDefault();
                            person.active = true;
                            person.fullName = model.FullName;
                            person.password = Helpers.SimpleHash.ComputeHash(model.Password, "MD5", null);
                            person.email = model.Email;
                        }
                        else
                        {
                            Person person = new Person()
                            {
                                user_id = model.UserName,
                                fullName = model.FullName,
                                password = Helpers.SimpleHash.ComputeHash(model.Password, "MD5", null),
                                email = model.Email,
                                joined = DateTime.Now,
                                active = true
                            };

                            _twitterCloneEntity.People.Add(person);
                        }
                        _twitterCloneEntity.SaveChanges();

                        return RedirectToAction("Index", "Home");
                    }
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ManageProfile()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageProfile(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    var userId = User.Identity.Name;

                    Person person = _twitterCloneEntity.People.Where(x => x.user_id == userId).Select(x => x).FirstOrDefault();
                    person.password = SimpleHash.ComputeHash(model.NewPassword, "MD5", null);
                    person.email = model.Email;
                    await _twitterCloneEntity.SaveChangesAsync();
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);

            return View(model);
        }

        public ActionResult DeleteAccount()
        {
            var userId = User.Identity.GetUserId();
            var userName = User.Identity.Name;
           
            using (ApplicationDbContext dbcontext = new ApplicationDbContext())
            {
                dbcontext.Users.Remove(dbcontext.Users.Where(usr => usr.Id == userId).Single());
                try
                {
                    dbcontext.SaveChanges();
                    Person person = _twitterCloneEntity.People.Where(x => x.user_id == userName).Select(x => x).FirstOrDefault();
                    person.active = false;
                    _twitterCloneEntity.SaveChanges();

                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError("deleteerror", ex);                    
                }
                return RedirectToAction("Login", "Account");
            }
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }
        #endregion
    }
}