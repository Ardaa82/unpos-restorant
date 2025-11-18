using System;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using FastPay.Presentation.ViewModels;
using Samba.Presentation.ViewModels;

namespace Samba.Modules.FastPay
{
    /// <summary>
    /// Interaction logic for FastTicketTotalsView.xaml
    /// </summary>

    [Export]
    public partial class FastTicketTotalsView : UserControl
    {
        [ImportingConstructor]
        public FastTicketTotalsView(FastTicketTotalsViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
