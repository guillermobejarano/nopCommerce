using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.MercadoPago
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="routeBuilder">Route builder</param>
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //PDT
            routeBuilder.MapRoute("Plugin.Payments.MercadoPago.SuccessOrder", "Plugins/PaymentMercadoPago/SuccessOrder",
                 new { controller = "PaymentMercadoPago", action = "SuccessOrder" });

            routeBuilder.MapRoute("Plugin.Payments.MercadoPago.CancelOrder", "Plugins/PaymentMercadoPago/CancelOrder",
                 new { controller = "PaymentMercadoPago", action = "CancelOrder" });

            routeBuilder.MapRoute("Plugin.Payments.MercadoPago.Notifications", "Plugins/PaymentMercadoPago/Notifications",
                 new { controller = "PaymentMercadoPago", action = "Notifications" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority
        {
            get { return -1; }
        }
    }
}
