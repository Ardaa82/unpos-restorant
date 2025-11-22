using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Services.Common;

namespace Samba.Modules.PackageServiceModule
{
    [ModuleExport(typeof(PackageServiceModule))]
    public class PackageServiceModule : VisibleModuleBase
    {
        private readonly IRegionManager _regionManager;
        private readonly PackageServiceView _packageServiceView;

        [ImportingConstructor]
        public PackageServiceModule(IRegionManager regionManager, PackageServiceView packageServiceView)
            : base(regionManager, AppScreens.PackageServiceView)
        {
            _regionManager = regionManager;
            _packageServiceView = packageServiceView;

            // Navigasyonda "Paket Servis" sekmesi
            SetNavigationCommand("Paket Servis", Resources.Common, "Images/sepet512.png", 15);
        }

        protected override void OnInitialization()
        {
            // Bu modülün ana görünümünü MainRegion'a ekle
            _regionManager.Regions[RegionNames.MainRegion]
                .Add(_packageServiceView, "PackageServiceView");
        }

        public override object GetVisibleView()
        {
            return _packageServiceView;
        }
    }
}
