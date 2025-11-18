using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Tickets;
using Samba.Presentation.Common;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Services.Common;

namespace Samba.Modules.FastPayModule
{
    [Export]
    public class FastPayCoordinator : IPartImportsSatisfiedNotification, IDisposable
    {
        private readonly IRegionManager _regionManager;
        private readonly IApplicationState _applicationState;
        private readonly ITicketService _ticketService;
        private readonly FastPayModuleViewModel _fastPayVm; // your FastPay VM
        private readonly IEventAggregator _ea = null;

        private SubscriptionToken _ticketEventToken;
        private SubscriptionToken _genericEventToken;
        private SubscriptionToken _ruleEventToken;

        [ImportingConstructor]
        public FastPayCoordinator(IRegionManager regionManager,
                                 IApplicationState applicationState,
                                 ITicketService ticketService,
                                 FastPayModuleViewModel fastPayVm)
        {
            _regionManager = regionManager;
            _applicationState = applicationState;
            _ticketService = ticketService;
            _fastPayVm = fastPayVm;
        }

        public void OnImportsSatisfied()
        {
            // Subscribe once here (OnInitialization equivalent)
            var ev = EventServiceFactory.EventService;
            // Payment processed events (rule events)
            _ruleEventToken = ev.GetEvent<GenericEvent<EventParameters<object>>>().Subscribe(OnRuleEvent);
            // Generic events for ActivateFastPayView, CloseTicketRequested, TicketClosed
            _genericEventToken = ev.GetEvent<GenericEvent<EventAggregator>>().Subscribe(OnGenericEvent);
            // Ticket typed events (like SetSelectedTicket / RefreshSelectedTicket)
            _ticketEventToken = ev.GetEvent<GenericEvent<Ticket>>().Subscribe(OnTicketEvent);
        }

        private void OnRuleEvent(EventParameters<EventParameters<object>> obj)
        {
            if (obj.Topic == RuleEventNames.PaymentProcessed)
            {
                _applicationState.IsPaymentDone = true;
            }
        }

        private void OnGenericEvent(EventParameters<EventAggregator> obj)
        {
            switch (obj.Topic)
            {
                case EventTopicNames.ActivateFastPayView:
                    _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
                    break;
                case EventTopicNames.CloseTicketRequested:
                    // Let TicketModule close the ticket. We just set waiting state if you want.
                    // Do nothing here.
                    break;
                case EventTopicNames.TicketClosed:
                    HandlePostTicketClosed();
                    break;
            }
        }

        private void OnTicketEvent(EventParameters<Ticket> obj)
        {
            if (obj.Topic == EventTopicNames.SetSelectedTicket || obj.Topic == EventTopicNames.RefreshSelectedTicket)
            {
                var t = obj.Value;
                // attach and reset UI
                _fastPayVm.Reset();
                _fastPayVm.AttachTicket(t);
            }
        }

        private void HandlePostTicketClosed()
        {
            if (!_applicationState.IsFastPayMode) return;

            if (_applicationState.IsPaymentDone)
            {
                // reset flag and create new ticket loop
                _applicationState.IsPaymentDone = false;
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.CreateTicket);
                // ActivateFastPayView once the new ticket is created (POS's CreateTicket handler will trigger RefreshSelectedTicket)
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
            }
            else
            {
                // No payment, go back to navigation
                _applicationState.IsFastPayMode = false;
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateNavigation);
            }
        }

        public void Dispose()
        {
            // Unsubscribe to avoid leaks
            var ev = EventServiceFactory.EventService;
            if (_ruleEventToken != null) ev.GetEvent<GenericEvent<EventParameters<object>>>().Unsubscribe(OnRuleEvent);
            if (_genericEventToken != null) ev.GetEvent<GenericEvent<EventAggregator>>().Unsubscribe(OnGenericEvent);
            if (_ticketEventToken != null) ev.GetEvent<GenericEvent<Ticket>>().Unsubscribe(OnTicketEvent);
        }
    }
}
