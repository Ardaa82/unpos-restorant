using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Modules.PaymentModule;
using Samba.Presentation.Common;
using Samba.Presentation.Services.Common;
using System.ComponentModel.Composition;

namespace Samba.Modules.FastPayModule
{
    [ModuleExport(typeof(FastPayPaymentModule))]
    class FastPayPaymentModule : VisibleModuleBase
    {
        private readonly IRegionManager _regionManager;
        private readonly PaymentEditorView _paymentEditorView;

        [ImportingConstructor]
        public FastPayPaymentModule(IRegionManager regionManager, PaymentEditorView paymentEditorView)
            : base(regionManager, AppScreens.PaymentView)
        {
            _regionManager = regionManager;
            _paymentEditorView = paymentEditorView;
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(PaymentEditorView));

            // FastPay ödeme tamamlandığında event publish
            EventServiceFactory.EventService.GetEvent<GenericEvent<string>>()
                .Subscribe(x =>
                {
                    if (x.Topic == EventTopicNames.TicketClosedFastPay)
                    {
                        EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivatePosViewFastPay);
                    }
                });
        }

        public override object GetVisibleView()
        {
            return _paymentEditorView;
        }
    }
}
