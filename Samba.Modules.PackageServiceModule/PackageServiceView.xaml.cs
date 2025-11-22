using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Samba.Modules.PackageServiceModule
{
    [Export]
    public partial class PackageServiceView : UserControl
    {
        [ImportingConstructor]
        public PackageServiceView(PackageServiceViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
