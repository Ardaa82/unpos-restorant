using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Services.Common;
using System;
using System.ComponentModel.Composition;

/* FastPayModule
 * Implements a Fast Pay module for the Samba POS system.
 * Ignores POS operations and provides a quick payment interface.
 * Has its own view and view model for handling, based off from ActivePostView. - ActivateFastPayView
 * Has a flag in ApplicationState to indicate Fast Pay mode.
 * The flag is set when navigating to the Fast Pay view and used to modify behavior elsewhere in the application.
 * FastPayMode is set to true when the Fast Pay view is activated and set to false when the ticket is closed or the user navigates away.
 * 
 */



namespace Samba.Modules.FastPayModule
{
    [ModuleExport(typeof(FastPayModule))]
    public class FastPayModule : VisibleModuleBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IUserService _userService;
        private readonly FastPayModuleView _fastPayModuleView;
        private readonly FastPayModuleViewModel _fastPayModuleViewModel;
        private readonly IApplicationState _applicationState;



        [ImportingConstructor]
        public FastPayModule(IRegionManager regionManager, IUserService userService,
            FastPayModuleView fastPayModuleView, FastPayModuleViewModel fastPayModuleViewModel, IApplicationState applicationState)
            : base(regionManager, AppScreens.FastPayView)
        {
            _regionManager = regionManager;
            _userService = userService;
            _fastPayModuleView = fastPayModuleView;
            _fastPayModuleViewModel = fastPayModuleViewModel;
            _applicationState = applicationState;

            SetNavigationCommand(Resources.FastPay, Resources.Common, "Images/sepet512.png", 50);
            PermissionRegistry.RegisterPermission(
                PermissionNames.OpenFastPay,
                PermissionCategories.Navigation,
                string.Format(Resources.CanNavigate_f, Resources.FastPay));
        }

        public void ExecuteFastPay()
        {
            _applicationState.IsPaymentDone = false;
            _applicationState.IsFastPayMode = true;
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.CreateTicket);



        }

        private void OnPaymentEvent(EventParameters<EventParameters<object>> obj)
        {
            if (obj.Topic == RuleEventNames.PaymentProcessed)
            {
                _applicationState.IsPaymentDone = true;
            }
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(FastPayModuleView));
            EventServiceFactory.EventService.GetEvent<GenericEvent<EventParameters<object>>>().Subscribe(OnPaymentEvent);
            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(OnEvent);
        }

        protected override bool CanNavigate(string arg)
        {
            return _userService.IsUserPermittedFor(PermissionNames.OpenFastPay);
        }

        protected override void OnNavigate(string obj)
        {
            base.OnNavigate(obj);
            ExecuteFastPay();

        }
        // Event handler for various events related to Fast Pay functionality.
        // If the ActivateFastPayView event is received, it navigates to the Fast Pay view.
        // If the CloseTicketRequested event is received, it exits Fast Pay mode and activates navigation.
        private void OnEvent(EventParameters<EventAggregator> obj)
        {
            switch (obj.Topic)
            {
                case EventTopicNames.ActivateFastPayView:
                    _regionManager.RequestNavigate(
                        RegionNames.MainRegion,
                        new Uri("FastPayView", UriKind.Relative));
                    break;

                case EventTopicNames.CloseTicketRequested:
                    // DO NOT redirect here.
                    // Let the TicketModule actually close the ticket.
                    // _applicationState.IsFastPayMode = false;
                    if (_applicationState.IsPaymentDone)
                    {

                        _regionManager.RequestNavigate(
                        RegionNames.MainRegion,
                        new Uri("FastPayView", UriKind.Relative));
                    }
                    else
                        {
                        // Unsubscribe if not in Fast Pay mode
                        EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateNavigation);
                    }
                        break;

              

            }
        }

        public override object GetVisibleView()
        {
            return _fastPayModuleView;
        }
    }
}
