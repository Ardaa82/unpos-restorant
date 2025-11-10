using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Samba.Domain.Models.Menus;
using Samba.Presentation.Common;
using Samba.Services;

namespace Unpos.Modules.QuickSaleModule
{
    [Export]
    public class QuickSaleModuleViewModel : ObservableObject
    {
        private readonly IMenuService _menuService;

        [ImportingConstructor]
        public QuickSaleModuleViewModel(IMenuService menuService)
        {
            _menuService = menuService;
            UrunleriYukle();
        }

        private ObservableCollection<MenuItem> _urunler;
        public ObservableCollection<MenuItem> Urunler
        {
            get => _urunler;
            set { _urunler = value; RaisePropertyChanged(() => Urunler); }
        }

        private void UrunleriYukle()
        {
            var menuItems = _menuService.GetMenuItems();
            Urunler = new ObservableCollection<MenuItem>(menuItems);
        }
    }
}
