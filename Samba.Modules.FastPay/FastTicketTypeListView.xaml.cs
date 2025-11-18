using System;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Samba.Modules.FastPay
{
    /// <summary>
    /// Interaction logic for FastTicketTypeListView.xaml
    /// </summary>

    [Export]
    public partial class FastTicketTypeListView : UserControl
    {
        [ImportingConstructor]
        public FastTicketTypeListView(FastTicketTypeListViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
