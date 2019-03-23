using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Services.Security;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using Nop.Web.Themes.TuTienda.Models.ShoppingCart;

namespace Nop.Web.Themes.TuTienda.Components
{
    [ViewComponent(Name = "FlyoutShoppingCartTuTienda")]
    public class FlyoutShoppingCart : NopViewComponent
    {
        private readonly IPermissionService _permissionService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public FlyoutShoppingCart(IPermissionService permissionService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            ShoppingCartSettings shoppingCartSettings)
        {
            this._permissionService = permissionService;
            this._shoppingCartModelFactory = shoppingCartModelFactory;
            this._shoppingCartSettings = shoppingCartSettings;
        }

        public IViewComponentResult Invoke(bool shoppingCartEnabled, int shoppingCartItems)
        {
            if (!_shoppingCartSettings.MiniShoppingCartEnabled)
                return Content("");

            if (!_permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
                return Content("");

            var miniShoppingCartModel = _shoppingCartModelFactory.PrepareMiniShoppingCartModel();

            var model = new MiniShoppingCartTuTiendaModel();
            model.Items = miniShoppingCartModel.Items;
            model.TotalProducts = miniShoppingCartModel.TotalProducts;
            model.SubTotal = string.IsNullOrEmpty(miniShoppingCartModel.SubTotal) 
                ? decimal.Zero.ToString()
                : miniShoppingCartModel.SubTotal;
            model.DisplayShoppingCartButton = miniShoppingCartModel.DisplayShoppingCartButton;
            model.DisplayCheckoutButton = miniShoppingCartModel.DisplayCheckoutButton;
            model.CurrentCustomerIsGuest = miniShoppingCartModel.CurrentCustomerIsGuest;
            model.AnonymousCheckoutAllowed = miniShoppingCartModel.AnonymousCheckoutAllowed;
            model.Form = miniShoppingCartModel.Form;
            model.ShoppingCartEnabled = shoppingCartEnabled;
            model.ShoppingCartItems = shoppingCartItems;
            model.ShowProductImages = miniShoppingCartModel.ShowProductImages;
            return View(model);
        }
    }
}
