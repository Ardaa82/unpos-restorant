using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Samba.Modules.ModifierModule
{
    [Export]
    public partial class FastTicketLogViewerView : UserControl
    {
        [ImportingConstructor]
        public FastTicketLogViewerView(FastTicketLogViewerViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
