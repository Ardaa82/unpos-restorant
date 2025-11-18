using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Services.Common;
using Samba.Services.Common;
using System.ComponentModel.Composition;

namespace Samba.Modules.FastPayModule.ActionProcessors
{
    [Export(typeof(IActionType))]
    class FastDisplayTicketList : ActionType
    {
        public override void Process(ActionData actionData)
        {
            var ticketTagName = actionData.GetAsString("TicketTagName");
            var ticketStateName = actionData.GetAsString("TicketStateName");

            if (!string.IsNullOrEmpty(ticketStateName))
            {
                var dt = new TicketStateData { StateName = ticketStateName };
                dt.PublishEvent(EventTopicNames.ActivateFastTicketList);
            }
            else if (!string.IsNullOrEmpty(ticketTagName))
            {
                var dt = new TicketTagGroup { Name = ticketTagName };
                dt.PublishEvent(EventTopicNames.ActivateFastTicketList);
            }
        }

        protected override object GetDefaultData()
        {
            return new { TicketTagName = "", TicketStateName = "" };
        }

        protected override string GetActionName()
        {
            return Resources.DisplayTicketList;
        }

        protected override string GetActionKey()
        {
            return ActionNames.DisplayTicketList;
        }
    }
}
