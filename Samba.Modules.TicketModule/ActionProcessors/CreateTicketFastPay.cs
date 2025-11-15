using System;
using System.ComponentModel.Composition;
using Samba.Localization.Properties;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Services.Common;

namespace Samba.Modules.PosModule.ActionProcessors
{
    [Export(typeof(IActionType))]
    internal class CreateTicketFastPay : ActionType
    {
        private readonly IApplicationState _applicationState;
        private readonly ITicketService _ticketService;

        [ImportingConstructor]
        public CreateTicketFastPay(IApplicationState applicationState, ITicketService ticketService)
        {
            _applicationState = applicationState;
            _ticketService = ticketService;
        }

        public override void Process(ActionData actionData)
        {
            // FastPay flag olarak kullanılacak
            bool isFastPay = true;

            // TicketId varsa onu aç
            var ticketId = actionData.GetDataValueAsInt("TicketId");
            if (ticketId > 0 && !_applicationState.IsLocked)
            {
                var ticket = _ticketService.OpenTicket(ticketId);
                ticket.PublishEvent(EventTopicNames.SetSelectedTicket);
            }

            // FastPay için özel event
            if (isFastPay)
            {
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.CreateTicketFastPay);
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivatePosViewFastPay);
            }
            else
            {
                // Normal akış
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.CreateTicket);
            }
        }

        protected override object GetDefaultData()
        {
            return new object();
        }

        protected override string GetActionName()
        {
            return string.Format(Resources.Create_f, Resources.Ticket + " (FastPay)");
        }

        protected override string GetActionKey()
        {
            return "CreateTicketFastPay";
        }
    }
}
