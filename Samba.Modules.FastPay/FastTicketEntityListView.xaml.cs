using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Samba.Modules.FastPayModule
{
    /// <summary>
    /// Interaction logic for FastTicketEntityListView.xaml
    /// </summary>

    [Export]
    public partial class FastTicketEntityListView : UserControl
    {
        [ImportingConstructor]
        public FastTicketEntityListView(FastTicketEntityListViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
