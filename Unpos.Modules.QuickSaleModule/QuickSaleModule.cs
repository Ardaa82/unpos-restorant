using System.ComponentModel.Composition;
using System.Windows.Controls;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;

namespace Unpos.Modules.QuickSaleModule
{
    [ModuleExport(typeof(QuickSaleModule))]

    public class QuickSaleModule : VisibleModuleBase
    {
        public string Foto = "D:\\Arda\\projeler\\unpos\\Samba.Modules.MarketModule\\Images\\sepet512.png";

        private readonly IRegionManager _regionManager;
        private readonly IUserService _userService;
        private readonly QuickSaleModuleView _view;
        private readonly QuickSaleModuleViewModel _viewModel;

        [ImportingConstructor]
        public QuickSaleModule(IRegionManager regionManager, IUserService userService, QuickSaleModuleView view, QuickSaleModuleViewModel viewModel)
            : base(regionManager, AppScreens.MarketView) // Aynı ekran kimliği kullanılabilir
        {
            _regionManager = regionManager;
            _userService = userService;
            _view = view;
            _viewModel = viewModel;

            // Sol menüde görünecek başlık ve ikon
            SetNavigationCommand("Hızlı Satış", Resources.Common, Foto, 20);
            PermissionRegistry.RegisterPermission("OpenQuickSale", PermissionCategories.Navigation, "Hızlı Satış modülüne erişim");
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(QuickSaleModuleView));
        }

        protected override bool CanNavigate(string arg)
        {
            return _userService.IsUserPermittedFor("OpenQuickSale");
        }

        protected override void OnNavigate(string obj)
        {
            base.OnNavigate(obj);
        }

        public override object GetVisibleView()
        {
            return _view;
        }
    }
}
