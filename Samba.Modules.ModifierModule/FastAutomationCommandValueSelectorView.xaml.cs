using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Samba.Modules.ModifierModule
{
    /// <summary>
    /// Interaction logic for FastAutomationCommandValueSelectorView.xaml
    /// </summary>
    [Export]
    public partial class FastAutomationCommandValueSelectorView : UserControl
    {
        [ImportingConstructor]
        public FastAutomationCommandValueSelectorView(FastAutomationCommandValueSelectorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
