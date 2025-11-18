using Samba.Domain.Models.Tickets;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Services.Common;
using System.ComponentModel.Composition;

namespace Unpos.Modules.QuickSale.ActionProcessors
{
    [Export(typeof(IActionType))]
    internal class SetSelectedTicketFastPay : ActionType
    {
        private readonly IApplicationState _applicationState;

        [ImportingConstructor]
        public SetSelectedTicketFastPay(IApplicationState applicationState)
        {
            _applicationState = applicationState;
        }

        public override void Process(ActionData actionData)
        {
            var ticket = actionData.GetDataValue<Ticket>("Ticket");
            if (ticket == null) return;

            // Store it in a FastPay-specific property
            _applicationState.CurrentFastPayTicket = ticket;

            // Only publish the event name — don't pass the ticket
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.SetSelectedTicketFastPay);
        }

        protected override object GetDefaultData()
        {
            return new { Ticket = (Ticket)null };
        }

        protected override string GetActionName()
        {
            return "Set Selected Ticket FastPay";
        }

        protected override string GetActionKey()
        {
            return "SetSelectedTicketFastPay";
        }
    }
}
