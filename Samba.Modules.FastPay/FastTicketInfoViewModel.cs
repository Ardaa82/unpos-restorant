using System;
using System.ComponentModel.Composition;
using System.Linq;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Common;

namespace Samba.Modules.FastPayModule
{
    [Export]
    public class FastTicketInfoViewModel : ObservableObject
    {
        public FastTicketInfoViewModel()
        {
            SelectedTicket = Ticket.Empty;
        }

        private Ticket _selectedTicket;
        public Ticket SelectedTicket
        {
            get { return _selectedTicket; }
            set
            {
                _selectedTicket = value;
                // Optionally call Refresh() when the ticket changes
            }
        }

        public bool IsTicketTagged => SelectedTicket.IsTagged;

        public string TicketTagDisplay =>
            SelectedTicket.GetTagData()
                .Split('\r')
                .Select(x => !string.IsNullOrEmpty(x) && x.Contains(":") && x.Split(':')[0].Trim() == x.Split(':')[1].Trim() ? x.Split(':')[0] : x)
                .Aggregate("", (c, v) => c + v + "\r")
                .Trim('\r');

        public bool IsTicketNoteVisible => !string.IsNullOrEmpty(Note);
        public string Note => SelectedTicket.Note;
        public bool IsTicketTimeVisible => SelectedTicket.Id != 0;
        public bool IsLastPaymentDateVisible => SelectedTicket.Payments.Count > 0;

        public bool IsLastOrderDateVisible =>
            SelectedTicket.Orders.Count > 1 &&
            SelectedTicket.Orders.Last().OrderNumber != 0 &&
            SelectedTicket.Orders.First().OrderNumber != SelectedTicket.Orders.Last().OrderNumber;

        public string TicketCreationDate
        {
            get
            {
                if (SelectedTicket.IsClosed) return SelectedTicket.Date.ToString();
                var time = SelectedTicket.GetTicketCreationMinuteStr();

                return !string.IsNullOrEmpty(time)
                    ? string.Format(Resources.TicketTimeDisplay_f, SelectedTicket.Date.ToShortTimeString(), time)
                    : SelectedTicket.Date.ToShortTimeString();
            }
        }

        public string TicketLastOrderDate
        {
            get
            {
                if (SelectedTicket.IsClosed) return SelectedTicket.LastOrderDate.ToString();
                var time = SelectedTicket.GetTicketLastOrderMinuteStr();

                return !string.IsNullOrEmpty(time)
                    ? string.Format(Resources.TicketTimeDisplay_f, SelectedTicket.LastOrderDate.ToShortTimeString(), time)
                    : SelectedTicket.LastOrderDate.ToShortTimeString();
            }
        }

        public string TicketLastPaymentDate
        {
            get
            {
                if (!SelectedTicket.IsClosed)
                    return SelectedTicket.LastPaymentDate != SelectedTicket.Date ? SelectedTicket.LastPaymentDate.ToShortTimeString() : "-";

                var time = new TimeSpan(SelectedTicket.LastPaymentDate.Ticks - SelectedTicket.Date.Ticks).TotalMinutes.ToString("#");
                return !string.IsNullOrEmpty(time)
                    ? string.Format(Resources.TicketTimeDisplay_f, SelectedTicket.LastPaymentDate, time)
                    : SelectedTicket.LastPaymentDate.ToString();
            }
        }

        public void Refresh()
        {
            RaisePropertyChanged(() => Note);
            RaisePropertyChanged(() => IsTicketNoteVisible);
            RaisePropertyChanged(() => IsTicketTagged);
            RaisePropertyChanged(() => TicketTagDisplay);
            RaisePropertyChanged(() => IsTicketTimeVisible);
            RaisePropertyChanged(() => IsLastPaymentDateVisible);
            RaisePropertyChanged(() => IsLastOrderDateVisible);
            RaisePropertyChanged(() => TicketCreationDate);
            RaisePropertyChanged(() => TicketLastOrderDate);
            RaisePropertyChanged(() => TicketLastPaymentDate);
        }
    }
}
