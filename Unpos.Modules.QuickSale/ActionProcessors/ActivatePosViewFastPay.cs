using System;
using System.ComponentModel.Composition;
using Samba.Presentation.Services.Common;
using Samba.Services.Common;

namespace Unpos.Modules.QuickSale.ActionProcessors
{
    [Export(typeof(IActionType))]
    class ActivatePosViewFastPay : ActionType
    {
        public override void Process(ActionData actionData)
        {
            // FastPay için POS ekranını aç
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivatePosViewFastPay);

        }

        protected override object GetDefaultData()
        {
            return null;
        }

        protected override string GetActionName()
        {
            return "Activate POS View FastPay";
        }

        protected override string GetActionKey()
        {
            return "ActivatePosViewFastPay";
        }
    }
}
