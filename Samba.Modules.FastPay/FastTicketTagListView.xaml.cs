using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Samba.Modules.FastPay
{
    [Export]
    public partial class FastTicketTagListView : UserControl
    {
        [ImportingConstructor]
        public FastTicketTagListView(FastTicketTagListViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
