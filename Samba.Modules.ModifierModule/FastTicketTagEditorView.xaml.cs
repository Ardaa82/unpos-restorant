using System.ComponentModel.Composition;
using System.Windows.Controls;
using Samba.Presentation.Common;

namespace Samba.Modules.ModifierModule
{
    [Export]
    public partial class FastTicketTagEditorView : UserControl
    {
        [ImportingConstructor]
        public FastTicketTagEditorView(FastTicketTagEditorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void GroupBox_IsVisibleChanged_2(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (((Control)sender).IsVisible)
                FreeTag.BackgroundFocus();
        }
    }
}
