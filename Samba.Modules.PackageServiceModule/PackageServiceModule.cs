using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Modules.EntityModule;
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

            SetNavigationCommand("Paket Servis", Resources.Common, "Images/sepet512.png", 15);
        }

        protected override void OnInitialization()
        {
            // VisibleModuleBase muhtemelen kendi view'ini MainRegion'a ekliyor,
            // o yüzden önce base'i çaðýr.
            base.OnInitialization();

            // PackageServiceView içindeki PackageServiceEntityRegion'a EntitySwitcherView'i kaydet
            _regionManager.RegisterViewWithRegion("PackageServiceEntityRegion", typeof(EntitySwitcherView));
        }

        public override object GetVisibleView()
        {
            return _packageServiceView;
        }
    }
}
