using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;

namespace Samba.Modules.ModifierModule
{
    [Export]
    public partial class FastOrderTagGroupEditorView : UserControl
    {
        [ImportingConstructor]
        public FastOrderTagGroupEditorView(FastOrderTagGroupEditorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                tb.Focus();
                tb.SelectAll();
            }
        }
    }
}
