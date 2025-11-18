using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Entities;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;

namespace Unpos.Modules.QuickSale
{
    [ModuleExport(typeof(QuickSale))]
    class QuickSale : VisibleModuleBase
    {
        private readonly QuickSaleView _quickSaleView;
        private readonly MenuItemSelectorView _menuItemSelectorView;
        private readonly TicketEntityListView _ticketEntityListView;
        private readonly TicketTypeListView _ticketTypeListView;
        private readonly IRegionManager _regionManager;
        private readonly IApplicationState _applicationState;
        private readonly TicketView _ticketView;
        private readonly TicketListView _ticketListView;
        private readonly TicketTagListView _ticketTagListView;

        [ImportingConstructor]
        public QuickSale(IRegionManager regionManager, IApplicationState applicationState,
            QuickSaleView QuickSaleView, TicketView ticketView, TicketListView ticketListView, TicketTagListView ticketTagListView,
            MenuItemSelectorView menuItemSelectorView, TicketEntityListView ticketEntityListView, TicketTypeListView ticketTypeListView)
            : base(regionManager, AppScreens.TicketView)
        {
            SetNavigationCommand("Hızlı Ödeme", Resources.Common, "Images/masa512.png", 15);

            _quickSaleView = QuickSaleView;
            _menuItemSelectorView = menuItemSelectorView;
            _ticketEntityListView = ticketEntityListView;
            _ticketTypeListView = ticketTypeListView;
            _regionManager = regionManager;
            _applicationState = applicationState;
            _ticketView = ticketView;
            _ticketListView = ticketListView;
            _ticketTagListView = ticketTagListView;

            EventServiceFactory.EventService.GetEvent<GenericEvent<Entity>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.PaymentRequestedForTicket) Activate();
                });
            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.RefreshSelectedTicket)
                    {
                        _quickSaleView.BackgroundFocus();
                    }
                });
        }

        protected override void OnInitialization()
        {
            _regionManager.Regions[RegionNames.MainRegion].Add(_quickSaleView, "QuickSaleView");
            _regionManager.Regions[RegionNames.PosMainRegion].Add(_ticketView, "TicketView");
            _regionManager.Regions[RegionNames.PosMainRegion].Add(_ticketListView, "TicketListView");
            _regionManager.Regions[RegionNames.PosMainRegion].Add(_ticketTagListView, "TicketTagListView");
            _regionManager.Regions[RegionNames.PosMainRegion].Add(_ticketEntityListView, "TicketEntityListView");
            _regionManager.Regions[RegionNames.PosMainRegion].Add(_ticketTypeListView, "TicketTypeListView");
            _regionManager.Regions[RegionNames.PosSubRegion].Add(_menuItemSelectorView, "MenuItemSelectorView");
            _regionManager.RegisterViewWithRegion(RegionNames.TicketOrdersRegion, typeof(TicketOrdersView));
            _regionManager.RegisterViewWithRegion(RegionNames.TicketInfoRegion, typeof(TicketInfoView));
            _regionManager.RegisterViewWithRegion(RegionNames.TicketTotalsRegion, typeof(TicketTotalsView));
        }

        protected override bool CanNavigate(string arg)
        {
            return _applicationState.IsCurrentWorkPeriodOpen;
        }

        protected override void OnNavigate(string obj)
        {
            base.OnNavigate(obj);
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateQuickSaleView);
        }

        public override object GetVisibleView()
        {
            return _quickSaleView;
        }
    }
}
