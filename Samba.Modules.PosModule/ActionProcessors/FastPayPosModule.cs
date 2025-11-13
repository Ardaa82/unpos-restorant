using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Modules.PosModule;
using Samba.Presentation.Common;
using Samba.Presentation.Services.Common;
using System.ComponentModel.Composition;

namespace Samba.Modules.FastPayModule
{
    [ModuleExport(typeof(FastPayPosModule))]
    class FastPayPosModule : VisibleModuleBase
    {
        private readonly PosView _posView;
        private readonly IRegionManager _regionManager;

        [ImportingConstructor]
        public FastPayPosModule(IRegionManager regionManager, PosView posView)
            : base(regionManager, AppScreens.TicketView)
        {
            _regionManager = regionManager;
            _posView = posView;
        }

        protected override void OnInitialization()
        {
            _regionManager.Regions[RegionNames.MainRegion].Add(_posView, "FastPayPosView");

            EventServiceFactory.EventService.GetEvent<GenericEvent<string>>()
                .Subscribe(x =>
                {
                    if (x.Topic == EventTopicNames.ActivatePosViewFastPay)
                        Activate();
                });
        }

        public override object GetVisibleView()
        {
            return _posView;
        }
    }
}
