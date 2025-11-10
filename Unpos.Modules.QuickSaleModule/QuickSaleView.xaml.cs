using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Unpos.Modules.QuickSaleModule
{
    [Export]
    public partial class QuickSaleModuleView : UserControl
    {
        [ImportingConstructor]
        public QuickSaleModuleView(QuickSaleModuleViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
