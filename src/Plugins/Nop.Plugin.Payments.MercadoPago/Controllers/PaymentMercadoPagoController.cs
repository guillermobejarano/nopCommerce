using MercadoPago;
using MercadoPago.Common;
using MercadoPago.Resources;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.MercadoPago.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System;
using System.IO;
using System.Text;

namespace Nop.Plugin.Payments.MercadoPago.Controllers
{
    public class PaymentMercadoPagoController : BasePaymentController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly INotificationService _notificationService;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;
        private readonly Preference _preferenceMercadoPago;
        private readonly MercadoPagoPaymentSettings _mercadoPagoPaymentSettings;

        #endregion

        #region Ctor

        public PaymentMercadoPagoController(ILocalizationService localizationService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            IStoreService storeService,
            INotificationService notificationService,
            IWorkContext workContext,
            IWebHelper webHelper,
            MercadoPagoPaymentSettings mercadoPagoPaymentSettings)
        {
            this._localizationService = localizationService;
            this._logger = logger;
            this._orderProcessingService = orderProcessingService;
            this._orderService = orderService;
            this._permissionService = permissionService;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._storeService = storeService;
            this._notificationService = notificationService;
            this._workContext = workContext;
            this._webHelper = webHelper;
            this._preferenceMercadoPago = new Preference();
            this._mercadoPagoPaymentSettings = mercadoPagoPaymentSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Create webhook that receive events for the subscribed event types
        /// </summary>
        /// <returns>Webhook id</returns>
        protected string CreateWebHook()
        {
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            
            return string.Empty;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var mercadoPagoPaymentSettings = _settingService.LoadSetting<MercadoPagoPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ClientId = mercadoPagoPaymentSettings.ClientId,
                ClientSecret = mercadoPagoPaymentSettings.ClientSecret,
                WebhookId = mercadoPagoPaymentSettings.WebhookId,
                UseSandbox = mercadoPagoPaymentSettings.UseSandbox,
                TransactModeId = (int)mercadoPagoPaymentSettings.TransactMode,
                //TransactModeValues = mercadoPagoPaymentSettings.TransactMode.ToSelectList(),
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.ClientId_OverrideForStore = _settingService.SettingExists(mercadoPagoPaymentSettings, x => x.ClientId, storeScope);
                model.ClientSecret_OverrideForStore = _settingService.SettingExists(mercadoPagoPaymentSettings, x => x.ClientSecret, storeScope);
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(mercadoPagoPaymentSettings, x => x.UseSandbox, storeScope);
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(mercadoPagoPaymentSettings, x => x.TransactMode, storeScope);
            }

            return View("~/Plugins/Payments.MercadoPago/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var mercadoPagoPaymentSettings = _settingService.LoadSetting<MercadoPagoPaymentSettings>(storeScope);

            //save settings
            mercadoPagoPaymentSettings.ClientId = model.ClientId;
            mercadoPagoPaymentSettings.ClientSecret = model.ClientSecret;
            mercadoPagoPaymentSettings.WebhookId = model.WebhookId;
            mercadoPagoPaymentSettings.UseSandbox = model.UseSandbox;
            mercadoPagoPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(mercadoPagoPaymentSettings, x => x.ClientId, model.ClientId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(mercadoPagoPaymentSettings, x => x.ClientSecret, model.ClientSecret_OverrideForStore, storeScope, false);
            _settingService.SaveSetting(mercadoPagoPaymentSettings, x => x.WebhookId, 0, false);
            _settingService.SaveSettingOverridablePerStore(mercadoPagoPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(mercadoPagoPaymentSettings, x => x.TransactMode, model.TransactModeId_OverrideForStore, storeScope, false);


            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("createwebhook")]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult GetWebhookId(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            return Configure();
        }

        [HttpPost]
        public IActionResult WebhookEventsHandler()
        {
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;

            try
            {
                var requestBody = string.Empty;
                using (var stream = new StreamReader(this.Request.Body, Encoding.UTF8))
                {
                    requestBody = stream.ReadToEnd();
                }


                return Ok();
            }
            catch (Exception ex)
            {
                return Ok();
            }
        }

        public IActionResult Notifications(string topic, long id)
        {
            try
            {
                if (SDK.ClientId == null)
                {
                    SDK.ClientId = this._mercadoPagoPaymentSettings.ClientId;
                }
            
                if (SDK.ClientSecret == null)
                {
                    SDK.ClientSecret = this._mercadoPagoPaymentSettings.ClientSecret;
                }
            
                var paymentMercadoPago = Payment.FindById(id);
                var order = this._orderService.GetOrderByGuid(new Guid(paymentMercadoPago.ExternalReference));
            
                switch (paymentMercadoPago.Status)
                {
                    case PaymentStatus.approved:
                        order.PaymentStatus = Core.Domain.Payments.PaymentStatus.Paid;
                        break;
                    case PaymentStatus.cancelled:
                        order.PaymentStatus = Core.Domain.Payments.PaymentStatus.Voided;
                        break;
                    case PaymentStatus.in_process:
                        order.PaymentStatus = Core.Domain.Payments.PaymentStatus.Pending;
                        break;
                    default:
                        break;
                }
            
                _orderService.UpdateOrder(order);
            }
            catch (Exception ex)
            {
                _logger.Error("Plugin MercadoPago Payment Notifications: ", ex);
            
                return Ok();
            }


            return Ok();
        }

        public IActionResult SuccessOrder(string external_reference)
        {
            var order = this._orderService.GetOrderByGuid(new Guid(external_reference));

            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("HomePage");
        }

        public IActionResult CancelOrder(string external_reference)
        {
            var order = this._orderService.GetOrderByGuid(new Guid(external_reference));

            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("HomePage");
        }
        #endregion
    }
}