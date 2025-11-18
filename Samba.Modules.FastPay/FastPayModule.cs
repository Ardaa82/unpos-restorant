using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using System.ComponentModel.Composition;
using Samba.Modules.FastPayModule;
using Samba.Modules.FastPay; // FastEntity tipi için gerekli olan using yönergesi eklendi

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
        public FastPayModule(IRegionManager regionManager, IApplicationState applicationState,
            FastPayView fastPayView, FastTicketView ticketView, FastTicketListView ticketListView, FastTicketTagListView ticketTagListView,
            FastMenuItemSelectorView menuItemSelectorView, FastTicketEntityListView ticketEntityListView, FastTicketTypeListView ticketTypeListView)
            : base(regionManager, AppScreens.FastPayView)
        {
            SetNavigationCommand(Resources.FastPay, Resources.Common, "Images/sepet512.png", 80);

            _fastPayView = fastPayView;
            _menuItemSelectorView = menuItemSelectorView;
            _ticketEntityListView = ticketEntityListView;
            _ticketTypeListView = ticketTypeListView;
            _regionManager = regionManager;
            _applicationState = applicationState;
            _ticketView = ticketView;
            _ticketListView = ticketListView;
            _ticketTagListView = ticketTagListView;

            // Custom FastPay events (replacing PosModule events)
            EventServiceFactory.EventService.GetEvent<GenericEvent<FastEntityButton>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.PaymentRequested) Activate();
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
            _regionManager.Regions[RegionNames.MainRegion].Add(_fastPayView, "FastPayView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketView, "FastTicketView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketListView, "FastTicketListView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketTagListView, "FastTicketTagListView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketEntityListView, "FastTicketEntityListView");
            _regionManager.Regions[RegionNames.FastPayMainRegion].Add(_ticketTypeListView, "FastTicketTypeListView");
            _regionManager.Regions[RegionNames.FastPaySubRegion].Add(_menuItemSelectorView, "FastMenuItemSelectorView");
            _regionManager.RegisterViewWithRegion(RegionNames.TicketOrdersRegion, typeof(FastTicketOrdersView));
            _regionManager.RegisterViewWithRegion(RegionNames.TicketInfoRegion, typeof(FastTicketInfoView));
            _regionManager.RegisterViewWithRegion(RegionNames.TicketTotalsRegion, typeof(FastTicketTotalsView));
        }

        protected override bool CanNavigate(string arg)
        {
            return _applicationState.IsCurrentWorkPeriodOpen;
        }

        protected override void OnNavigate(string obj)
        {
            base.OnNavigate(obj);
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
        }

        public override object GetVisibleView()
        {
            return _fastPayView;
        }
    }
}
