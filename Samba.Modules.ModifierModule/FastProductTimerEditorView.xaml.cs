using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Samba.Modules.ModifierModule
{
    [Export]
    public partial class FastProductTimerEditorView : UserControl
    {
        [ImportingConstructor]
        public FastProductTimerEditorView(FastProductTimerEditorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
