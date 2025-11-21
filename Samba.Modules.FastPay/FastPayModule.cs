using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using System.ComponentModel.Composition;
using Samba.Modules.FastPayModule;
using Samba.Modules.FastPay; // FastTicketTotalsView için

namespace Samba.Modules.FastPayModule
{
    [ModuleExport(typeof(FastPayModule))]
    class FastPayModule : VisibleModuleBase
    {
        private readonly FastPayView _fastPayView;
        private readonly FastMenuItemSelectorView _menuItemSelectorView;
        private readonly FastTicketEntityListView _ticketEntityListView;
        private readonly FastTicketTypeListView _ticketTypeListView;
        private readonly IRegionManager _regionManager;
        private readonly IApplicationState _applicationState;
        private readonly FastTicketView _ticketView;
        private readonly FastTicketListView _ticketListView;
        private readonly FastTicketTagListView _ticketTagListView;

        [ImportingConstructor]
        public FastPayModule(
            IRegionManager regionManager,
            IApplicationState applicationState,
            FastPayView fastPayView,
            FastTicketView ticketView,
            FastTicketListView ticketListView,
            FastTicketTagListView ticketTagListView,
            FastMenuItemSelectorView menuItemSelectorView,
            FastTicketEntityListView ticketEntityListView,
            FastTicketTypeListView ticketTypeListView)
            : base(regionManager, AppScreens.FastPayView)
        {
            SetNavigationCommand(Resources.FastPay, Resources.Common, "Images/sepet512.png", 20);

            _fastPayView = fastPayView;
            _menuItemSelectorView = menuItemSelectorView;
            _ticketEntityListView = ticketEntityListView;
            _ticketTypeListView = ticketTypeListView;
            _regionManager = regionManager;
            _applicationState = applicationState;
            _ticketView = ticketView;
            _ticketListView = ticketListView;
            _ticketTagListView = ticketTagListView;

            // FastPay’e özel event’ler
            EventServiceFactory.EventService.GetEvent<GenericEvent<FastEntityButton>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.PaymentRequested)
                        Activate();
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.RefreshSelectedFastTicket)
                    {
                        _fastPayView.BackgroundFocus();
                    }
                });
        }

        protected override void OnInitialization()
        {
            // Ana layout
            _regionManager.Regions[RegionNames.MainRegion].Add(_fastPayView, "FastPayView");

            // FastPay ana region (ticket tarafı)
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketView, "FastTicketView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketListView, "FastTicketListView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketTagListView, "FastTicketTagListView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketEntityListView, "FastTicketEntityListView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketTypeListView, "FastTicketTypeListView");

            // FastPay alt region (menü tarafı)
            _regionManager.Regions[RegionNames.FastPaySubRegion].Add(_menuItemSelectorView, "FastMenuItemSelectorView");

            // POS region’larını ezmek yerine FastPay için ayrı region isimleri kullanıyoruz
            _regionManager.RegisterViewWithRegion(RegionNames.FastTicketOrdersRegion, typeof(FastTicketOrdersView));
            _regionManager.RegisterViewWithRegion(RegionNames.FastTicketInfoRegion, typeof(FastTicketInfoView));
            _regionManager.RegisterViewWithRegion(RegionNames.FastTicketTotalsRegion, typeof(FastTicketTotalsView));
        }


        protected override void OnNavigate(string obj)
        {
            base.OnNavigate(obj);

            _applicationState.IsFastPayMode = true;
            _applicationState.IsPaymentDone = false;

            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
        }

        public override object GetVisibleView()
        {
            return _fastPayView;
        }
    }
}
