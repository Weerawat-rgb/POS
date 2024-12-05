using Microsoft.AspNetCore.Mvc.Rendering;

namespace POS.Web.Helpers
{
    public static class NavigationHelper
    {
        public static string IsActive(this IHtmlHelper htmlHelper, string? controller = null, string? action = null)
        {
            var currentController = htmlHelper.ViewContext.RouteData.Values["Controller"]?.ToString() ?? string.Empty;
            var currentAction = htmlHelper.ViewContext.RouteData.Values["Action"]?.ToString() ?? string.Empty;

            controller ??= currentController;
            action ??= currentAction;

            return string.Equals(controller, currentController, StringComparison.OrdinalIgnoreCase) && 
                   string.Equals(action, currentAction, StringComparison.OrdinalIgnoreCase) 
                   ? "active" 
                   : string.Empty;
        }
    }
}