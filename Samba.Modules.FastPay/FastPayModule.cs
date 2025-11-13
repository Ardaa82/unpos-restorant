using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using System;
using System.ComponentModel.Composition;

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

            SetNavigationCommand(Resources.FastPay, Resources.Common, "Images/hizli_satis.png", 50);
            PermissionRegistry.RegisterPermission(
                PermissionNames.OpenFastPay,
                PermissionCategories.Navigation,
                string.Format(Resources.CanNavigate_f, Resources.FastPay));
        }

        public void ExecuteFastPay()
        {
            _applicationState.IsFastPayMode = true;
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.CreateTicket);
            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(OnEvent);


        }
        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(FastPayModuleView));
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
        private void OnEvent(EventParameters<EventAggregator> obj)
        {
            if (obj.Topic == EventTopicNames.ActivateFastPayView)
            {
                // navigate to FastPay screen
                _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
            }
        }
        public override object GetVisibleView()
        {
            return _fastPayModuleView;
        }
    }
}
