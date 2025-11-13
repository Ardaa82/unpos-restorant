using System;
using System.ComponentModel.Composition;
using Samba.Domain.Models.Tickets;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Services.Common;

namespace Samba.Modules.PosModule.ActionProcessors
{
    [Export(typeof(IActionType))]
    internal class LoadFastPayOrderData : ActionType
    {
        private readonly IApplicationState _applicationState;
        private readonly ITicketService _ticketService;

        [ImportingConstructor]
        public LoadFastPayOrderData(IApplicationState applicationState, ITicketService ticketService)
        {
            _applicationState = applicationState;
            _ticketService = ticketService;
        }

        public override void Process(ActionData actionData)
        {
            // FastPay ticket'i al
            var ticket = _applicationState.CurrentFastPayTicket;
            if (ticket == null) return;

            // Ticket verilerini yenile (gerekirse)
            var refreshedTicket = _ticketService.OpenTicket(ticket.Id);

            // Event zinciri: Ticket yüklendi
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.LoadFastPayOrderData);

            // Sonraki adım: Ödeme ekranını aç
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivatePaymentScreenFastPay);
        }

        protected override object GetDefaultData()
        {
            return new { Ticket = (Ticket)null };
        }

        protected override string GetActionName()
        {
            return "Load FastPay Order Data";
        }

        protected override string GetActionKey()
        {
            return "LoadFastPayOrderData";
        }
    }
}
