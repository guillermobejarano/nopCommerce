using Nop.Web.Models.ShoppingCart;

namespace Nop.Web.Themes.TuTienda.Models.ShoppingCart
{
    public class MiniShoppingCartTuTiendaModel : MiniShoppingCartModel
    {
        public bool ShoppingCartEnabled { get; set; }
        public int ShoppingCartItems { get; set; }
    }
}
