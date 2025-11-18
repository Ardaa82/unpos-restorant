using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Services.Common;
using Samba.Services.Common;
using System.ComponentModel.Composition;

namespace Samba.Modules.FastPayModule.ActionProcessors
{
    [Export(typeof(IActionType))]
    class FastDisplayTicketLog : ActionType
    {
        public override void Process(ActionData actionData)
        {
            var ticket = actionData.GetDataValue<Ticket>("Ticket");
            if (ticket != null)
            {
                ticket.PublishEvent(EventTopicNames.DisplayFastTicketLog);
            }
        }

        protected override object GetDefaultData()
        {
            return null;
        }

        protected override string GetActionName()
        {
            return Resources.DisplayTicketLog;
        }

        protected override string GetActionKey()
        {
            return "DisplayTicketLog";
        }
    }
}
