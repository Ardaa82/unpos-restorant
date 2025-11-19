using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Samba.Modules.ModifierModule
{
    /// <summary>
    /// Interaction logic for FastAutomationCommandSelectorView.xaml
    /// </summary>
    [Export]
    public partial class FastAutomationCommandSelectorView : UserControl
    {
        [ImportingConstructor]
        public FastAutomationCommandSelectorView(FastAutomationCommandSelectorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        // Parameterless ctor WPF designer için; MEF kullanırken normalde çağrılmaz.
        public FastAutomationCommandSelectorView()
        {
            InitializeComponent();
        }
    }
}
