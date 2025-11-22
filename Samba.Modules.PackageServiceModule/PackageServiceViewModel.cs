using System.ComponentModel.Composition;
using Samba.Presentation.Common;

namespace Samba.Modules.PackageServiceModule
{
    [Export]
    public class PackageServiceViewModel : ObservableObject
    {
        [ImportingConstructor]
        public PackageServiceViewModel()
        {
        }

        public string HeaderText
        {
            get { return "Paket Servis"; }
        }

        public string Description
        {
            get { return "Telefonla gelen siparişleri hızlıca oluşturmak için masa ekranının yeniden kullanıldığı paket servis modu."; }
        }
    }
}
