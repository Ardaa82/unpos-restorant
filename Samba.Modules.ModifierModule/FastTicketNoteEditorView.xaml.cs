using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Samba.Presentation.Common;

namespace Samba.Modules.ModifierModule
{
    [Export]
    public partial class FastTicketNoteEditorView : UserControl
    {
        [ImportingConstructor]
        public FastTicketNoteEditorView(FastTicketNoteEditorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void TicketNoteEditorView_OnLoaded(object sender, RoutedEventArgs e)
        {
            TicketNote.BackgroundFocus();
            TicketNote.CaretIndex = TicketNote.Text.Length;
        }
    }
}
