using Samba.Presentation.Services.Common;
using Samba.Services.Common;
using System.ComponentModel.Composition;

namespace Samba.Modules.PosModule.ActionProcessors
{
    [Export(typeof(IActionType))]
    internal class ActivatePaymentScreenFastPay : ActionType
    {
        public override void Process(ActionData actionData)
        {
            // FastPay ticket seçildikten ve order'lar yüklendikten sonra çağrılır.
            // UI FastPay ödeme ekranına geçiş yapabilir.
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivatePaymentScreenFastPay);
        }

        protected override object GetDefaultData()
        {
            return new { };
        }

        protected override string GetActionName()
        {
            return "Activate Payment Screen FastPay";
        }

        protected override string GetActionKey()
        {
            return "ActivatePaymentScreenFastPay";
        }
    }
}
