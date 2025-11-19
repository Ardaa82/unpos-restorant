using System.ComponentModel.Composition;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Commands;
using Samba.Presentation.Services.Common;

namespace Samba.Modules.ModifierModule
{
    [Export]
    public class FastTicketNoteEditorViewModel : ObservableObject
    {
        private Ticket _selectedTicket;
        public Ticket SelectedTicket
        {
            get { return _selectedTicket; }
            set
            {
                _selectedTicket = value;
                RaisePropertyChanged(() => SelectedTicket);
            }
        }

        public ICaptionCommand CloseCommand { get; set; }

        public FastTicketNoteEditorViewModel()
        {
            CloseCommand = new CaptionCommand<string>(Resources.Close, OnClose);
        }

        private void OnClose(string obj)
        {
            SelectedTicket = null;
            // FastPay sürümü: POS yerine FastPay ekranına dön
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
        }
    }
}
