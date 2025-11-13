using Samba.Domain.Models.Tickets;
using Samba.Presentation.Services.Common;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Samba.Modules.FastPayModule
{
    [Export]
    public partial class FastPayModuleView : UserControl
    {
        [ImportingConstructor]
        public FastPayModuleView(FastPayModuleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void FastPayButton_Click(object sender, RoutedEventArgs e)
        {
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.CreateTicket);
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivatePosView);
       
        }
    }
}
