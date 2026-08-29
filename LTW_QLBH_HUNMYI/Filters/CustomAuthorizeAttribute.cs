using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace LTW_QLBH_HUNMYI.Filters
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public string[] AllowedRoles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.Session["UserID"] == null)
            {
                return false;
            }

            string userRole = httpContext.Session["Role"]?.ToString();

            if (AllowedRoles != null && AllowedRoles.Length > 0)
            {
                foreach (string role in AllowedRoles)
                {
                    if (userRole == role)
                    {
                        return true;
                    }
                }
                return false;
            }

            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Session["UserID"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new { controller = "Account", action = "Login" })
                );
            }
            else
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new { controller = "Account", action = "AccessDenied" })
                );
            }
        }
    }
}