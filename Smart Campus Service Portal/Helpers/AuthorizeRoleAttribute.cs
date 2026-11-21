using System;
using System.Linq;
using System.Web.Mvc;
using Smart_Campus_Service_Portal.Models.ViewModels;

namespace Smart_Campus_Service_Portal.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _allowedRoles = roles ?? new string[0];
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var request = httpContext.Request;

            bool isApi = request.RawUrl.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
                         request.Headers["Accept"]?.Contains("application/json") == true ||
                         request.IsAjaxRequest();

            // Check API Bearer token if present
            var authHeader = request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring(7).Trim();
                var user = AuthSessionHelper.ValidateApiToken(token);
                if (user != null)
                {
                    AuthSessionHelper.SignIn(user);
                }
            }

            if (!AuthSessionHelper.IsAuthenticated)
            {
                if (isApi)
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = ApiResponse<object>.Fail("Unauthorized. Please log in or provide a valid token."),
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                    filterContext.HttpContext.Response.StatusCode = 401;
                }
                else
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary
                        {
                            { "controller", "Account" },
                            { "action", "Login" },
                            { "returnUrl", request.RawUrl }
                        });
                }
                return;
            }

            if (_allowedRoles.Length > 0)
            {
                var currentRole = AuthSessionHelper.CurrentUserRole;
                bool isAuthorized = _allowedRoles.Any(r => string.Equals(r, currentRole, StringComparison.OrdinalIgnoreCase));

                if (!isAuthorized)
                {
                    if (isApi)
                    {
                        filterContext.Result = new JsonResult
                        {
                            Data = ApiResponse<object>.Fail("Forbidden. You do not have permission to access this resource."),
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                        filterContext.HttpContext.Response.StatusCode = 403;
                    }
                    else
                    {
                        filterContext.Controller.TempData["ErrorMessage"] = "You do not have permission to access this section.";
                        filterContext.Result = new RedirectToRouteResult(
                            new System.Web.Routing.RouteValueDictionary
                            {
                                { "controller", "Home" },
                                { "action", "Index" }
                            });
                    }
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
