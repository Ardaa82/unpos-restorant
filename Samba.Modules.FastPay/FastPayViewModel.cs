using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Messaging;
using Samba.Localization.Properties;
using Samba.Modules.FastPay;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Presentation.ViewModels;
using Samba.Services;
using Samba.Services.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Samba.Modules.FastPayModule
{
    [Export]
    public class FastPayViewModel : ObservableObject
    {
        private readonly ITicketService _ticketService;
        private readonly IUserService _userService;
        private readonly ICacheService _cacheService;
        private readonly IMessagingService _messagingService;
        private readonly IApplicationState _applicationState;
        private readonly IApplicationStateSetter _applicationStateSetter;
        private readonly IRegionManager _regionManager;

        private readonly FastMenuItemSelectorViewModel _fastMenuItemSelectorViewModel;
        private readonly FastTicketListViewModel _fastTicketListViewModel;
        private readonly FastTicketTagListViewModel _fastTicketTagListViewModel;
        private readonly AccountBalances _accountBalances;

        private readonly FastMenuItemSelectorView _fastMenuItemSelectorView;
        private readonly FastTicketViewModel _fastTicketViewModel;
        private readonly FastTicketOrdersViewModel _fastTicketOrdersViewModel;

        protected Action ExpectedAction { get; set; }

        private Ticket _selectedTicket;
        public Ticket SelectedTicket
        {
            get => _selectedTicket;
            set
            {
                _selectedTicket = value;
                _fastTicketViewModel.SelectedTicket = value;
                _fastMenuItemSelectorViewModel.SelectedTicket = value;

                var screenMenuId = _applicationState.CurrentDepartment.ScreenMenuId;
                if (value != null)
                {
                    _accountBalances.SelectedTicket = value;
                    _accountBalances.Refresh();
                    if (screenMenuId == 0)
                    {
                        var template = _cacheService.GetTicketTypeById(SelectedTicket.TicketTypeId);
                        if (template != null)
                            screenMenuId = template.GetScreenMenuId(_applicationState.CurrentTerminal);
                    }
                }
                else
                {
                    if (screenMenuId == 0 && _applicationState.CurrentTicketType != null)
                        screenMenuId = _applicationState.CurrentTicketType.ScreenMenuId;
                }

                _fastMenuItemSelectorViewModel.UpdateCurrentScreenMenu(screenMenuId);
                RaisePropertyChanged(() => SelectedTicket);
            }
        }

        [ImportingConstructor]
        public FastPayViewModel(
            IRegionManager regionManager,
            IApplicationState applicationState,
            IApplicationStateSetter applicationStateSetter,
            ITicketService ticketService,
            IUserService userService,
            ICacheService cacheService,
            IMessagingService messagingService,
            FastTicketListViewModel fastTicketListViewModel,
            FastTicketTagListViewModel fastTicketTagListViewModel,
            FastMenuItemSelectorViewModel fastMenuItemSelectorViewModel,
            FastMenuItemSelectorView fastMenuItemSelectorView,
            FastTicketViewModel fastTicketViewModel,
            FastTicketOrdersViewModel fastTicketOrdersViewModel,
            AccountBalances accountBalances)
        {
            _ticketService = ticketService;
            _userService = userService;
            _cacheService = cacheService;
            _messagingService = messagingService;
            _applicationState = applicationState;
            _applicationStateSetter = applicationStateSetter;
            _regionManager = regionManager;

            _fastMenuItemSelectorViewModel = fastMenuItemSelectorViewModel;
            _fastTicketListViewModel = fastTicketListViewModel;
            _fastTicketTagListViewModel = fastTicketTagListViewModel;
            _accountBalances = accountBalances;

            _fastMenuItemSelectorView = fastMenuItemSelectorView;
            _fastTicketViewModel = fastTicketViewModel;
            _fastTicketOrdersViewModel = fastTicketOrdersViewModel;

            // EVENT SUBSCRIPTIONS
            EventServiceFactory.EventService.GetEvent<GenericEvent<Order>>()
                .Subscribe(OnOrderEventReceived);

            EventServiceFactory.EventService.GetEvent<GenericEvent<Ticket>>()
                .Subscribe(OnTicketEventReceived);

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>()
                .Subscribe(OnTicketEvent);

            EventServiceFactory.EventService.GetEvent<GenericEvent<ScreenMenuItemData>>()
                .Subscribe(OnMenuItemSelected);

            EventServiceFactory.EventService.GetEvent<GenericIdEvent>()
                .Subscribe(OnTicketIdPublished);

            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketTagGroup>>()
                .Subscribe(OnTicketTagSelected);

            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketStateData>>()
                .Subscribe(OnTicketStateSelected);

            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketType>>()
                .Subscribe(OnTicketTypeChanged);

            // Screen menu cache reset
            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>()
                .Subscribe(x =>
                {
                    if (x.Topic == EventTopicNames.ResetCache &&
                        _applicationState.CurrentTicketType != null)
                    {
                        _fastMenuItemSelectorViewModel.Reset();
                        _fastMenuItemSelectorViewModel.UpdateCurrentScreenMenu(
                            _applicationState.CurrentTicketType.GetScreenMenuId(_applicationState.CurrentTerminal));
                    }
                });
        }

        #region Event handlers

        private void OnOrderEventReceived(EventParameters<Order> obj)
        {
            if (obj.Topic == EventTopicNames.OrderAdded &&
                obj.Value != null &&
                SelectedTicket != null)
            {
                _fastTicketOrdersViewModel.SelectedTicket = SelectedTicket;
                DisplaySingleTicket();
            }
        }

        private void OnTicketTypeChanged(EventParameters<TicketType> obj)
        {
            if ((obj.Topic == EventTopicNames.TicketTypeChanged ||
                 obj.Topic == EventTopicNames.TicketTypeSelected) &&
                obj.Value != null)
            {
                _fastMenuItemSelectorViewModel
                    .UpdateCurrentScreenMenu(obj.Value.GetScreenMenuId(_applicationState.CurrentTerminal));
            }
        }

        private void OnTicketStateSelected(EventParameters<TicketStateData> obj)
        {
            if (obj.Topic == EventTopicNames.ActivateTicketList)
            {
                if (SelectedTicket != null) CloseTicket();
                _fastTicketListViewModel.UpdateListByTicketState(obj.Value);
                if (_fastTicketListViewModel.Tickets.Any())
                    DisplayTicketList();
                else
                    DisplayTickets();
            }
        }

        private void OnTicketTagSelected(EventParameters<TicketTagGroup> obj)
        {
            if (obj.Topic == EventTopicNames.ActivateTicketList)
            {
                if (SelectedTicket != null) CloseTicket();
                _fastTicketListViewModel.UpdateListByTicketTagGroup(obj.Value);
                if (_fastTicketListViewModel.Tickets.Any())
                    DisplayTicketList();
                else
                    DisplayTickets();
            }
        }

        private void OnTicketEventReceived(EventParameters<Ticket> obj)
        {
            if (obj.Topic == EventTopicNames.SetSelectedTicket)
            {
                if (SelectedTicket != null) CloseTicket();
                _applicationStateSetter.SetApplicationLocked(true);
                SelectedTicket = obj.Value;
            }

            if (obj.Topic == EventTopicNames.MoveSelectedOrders &&
                SelectedTicket != null)
            {
                var newTicketId = _ticketService
                    .MoveOrders(SelectedTicket, SelectedTicket.ExtractSelectedOrders().ToArray(), 0)
                    .TicketId;

                SelectedTicket = null;
                OpenTicket(newTicketId);
                DisplaySingleTicket();
            }
        }

        private void OnTicketIdPublished(EventParameters<int> obj)
        {
            if (obj.Topic == EventTopicNames.DisplayTicket)
            {
                if (SelectedTicket != null) CloseTicket();
                if (SelectedTicket != null) return;

                ExpectedAction = obj.ExpectedAction;
                OpenTicket(obj.Value);
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
            }
        }

        private void OnMenuItemSelected(EventParameters<ScreenMenuItemData> obj)
        {
            if (obj.Topic == EventTopicNames.ScreenMenuItemDataSelected)
            {
                if (SelectedTicket == null)
                {
                    // FastPay: ürün seçildiği anda direkt yeni ticket aç
                    OpenTicket(0);
                }

                Debug.Assert(SelectedTicket != null);
                _fastTicketOrdersViewModel.AddOrder(obj.Value);
                DisplaySingleTicket();
            }
        }

        private void OnTicketEvent(EventParameters<EventAggregator> obj)
        {
            // FastPay dışındayken SADECE FastPay’i aktive eden event’e cevap ver
            if (!_applicationState.IsFastPayMode &&
                obj.Topic != EventTopicNames.ActivateFastPayView)
                return;

            switch (obj.Topic)
            {
                case EventTopicNames.CreateTicket:
                    CreateTicket();
                    EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                    break;

                case EventTopicNames.ActivateFastPayView:
                    _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);
                    _applicationState.IsFastPayMode = true;

                    if (SelectedTicket == null || _ticketService.CanDeselectOrders(SelectedTicket.SelectedOrders))
                    {
                        DisplayTickets();
                        DisplayMenuScreen();
                        _fastTicketViewModel.ResetTicket();
                    }
                    break;

                case EventTopicNames.RegenerateSelectedTicket:
                    if (SelectedTicket != null)
                    {
                        _fastTicketViewModel.ResetTicket();
                        DisplaySingleTicket();
                    }
                    break;

                case EventTopicNames.RefreshSelectedTicket:
                    DisplayMenuScreen();
                    DisplaySingleTicket();
                    break;

                case EventTopicNames.CloseTicketRequested:
                    DisplayMenuScreen();
                    CloseTicket();
                    break;
            }
        }



        #endregion

        #region Public API

        public void DisplayTickets()
        {
            // FastPay: entity ekranı yok, direkt ticket’a gir
            if (SelectedTicket == null)
            {
                OpenTicket(0);
            }

            _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);
            DisplaySingleTicket();
        }

        private void DisplaySingleTicket()
        {
            _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);

            if (ShouldDisplayTicketTagList(SelectedTicket))
            {
                _fastTicketTagListViewModel.Update(SelectedTicket);
                DisplayTicketTagList();
                return;
            }

            if (SelectedTicket != null &&
                !_userService.IsUserPermittedFor(PermissionNames.DisplayOtherWaitersTickets) &&
                SelectedTicket.Orders.Any() &&
                SelectedTicket.Orders[0].CreatingUserName != _applicationState.CurrentLoggedInUser.Name)
            {
                InteractionService.UserIntraction.GiveFeedback("Can't display this ticket");
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.CloseTicketRequested);
                return;
            }

            _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
            _regionManager.RequestNavigate(RegionNames.FastPayMainRegion, new Uri("FastTicketView", UriKind.Relative));

            _accountBalances.RefreshAsync(() => _fastTicketViewModel.RefreshSelectedTicketTitle());
            _fastTicketViewModel.RefreshSelectedItems();

            if (SelectedTicket != null)
            {
                CommonEventPublisher.ExecuteEvents(SelectedTicket);
            }
        }

        private bool ShouldDisplayTicketTagList(Ticket ticket)
        {
            return ticket != null
                   && ticket.Orders.Count == 0
                   && _applicationState.GetTicketTagGroups()
                       .Any(x => x.AskBeforeCreatingTicket && !ticket.IsTaggedWith(x.Name));
        }

        private void DisplayTicketList()
        {
            _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);
            _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
            _regionManager.RequestNavigate(RegionNames.FastPayMainRegion, new Uri("FastTicketListView", UriKind.Relative));
        }

        private void DisplayTicketTagList()
        {
            _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);
            _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
            _regionManager.RequestNavigate(RegionNames.FastPayMainRegion, new Uri("FastTicketTagListView", UriKind.Relative));
        }

        public void DisplayMenuScreen()
        {
            _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
            _regionManager.RequestNavigate(RegionNames.FastPaySubRegion, new Uri("FastMenuItemSelectorView", UriKind.Relative));
        }

        public bool HandleTextInput(string text)
        {
            return _regionManager.Regions[RegionNames.FastPaySubRegion]
                       .ActiveViews.Contains(_fastMenuItemSelectorView)
                   && _fastMenuItemSelectorViewModel.HandleTextInput(text);
        }

        public void OpenTicket(int id)
        {
            _applicationStateSetter.SetApplicationLocked(true);
            SelectedTicket = _ticketService.OpenTicket(id);
        }

        #endregion

        #region Close & helpers

        private void CreateTicket()
        {
            // Entitiesiz FastPay: sadece yeni ticket oluştur
            if (SelectedTicket != null)
            {
                CloseTicket();
                if (SelectedTicket != null) return;
            }

            OpenTicket(0);
        }

        private void CloseTicket()
        {
            if (SelectedTicket == null) return;

            // KAPATMADAN ÖNCE satışın durumu:
            var hadOrders = SelectedTicket.Orders.Any();
            var fullyPaid = hadOrders && SelectedTicket.GetRemainingAmount() == 0;

            // Kapatmaya izin verilmiyorsa hiç devam etme
            if (!SelectedTicket.CanCloseTicket() &&
                !SelectedTicket.IsTaggedWithDefinedTags(_cacheService.GetTicketTagGroupNames()))
            {
                return;
            }

            // Zorunlu tag kontrolleri vs.
            if (_fastTicketOrdersViewModel.Orders.Count > 0 &&
                SelectedTicket.GetRemainingAmount() == 0)
            {
                var message = GetPrintError();
                if (!string.IsNullOrEmpty(message))
                {
                    _fastTicketOrdersViewModel.ClearSelectedOrders();
                    _fastTicketViewModel.RefreshVisuals();
                    InteractionService.UserIntraction.GiveFeedback(message);
                    return;
                }
            }

            _fastTicketOrdersViewModel.ClearSelectedOrders();

            var result = _ticketService.CloseTicket(SelectedTicket);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                InteractionService.UserIntraction.GiveFeedback(result.ErrorMessage);
            }

            // Bu noktada ticket DB tarafında kapandı
            SelectedTicket = null;

            if (_applicationState.CurrentTerminal.AutoLogout)
            {
                _userService.LogoutUser(false);
            }
            else
            {
                if (ExpectedAction != null)
                {
                    ExpectedAction.Invoke();
                }
                else
                {
                    if (fullyPaid)
                    {
                        // Başarılı, ödenmiş satış -> FastPay’e geri dön (yeni satışa hazır ekran)
                        EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
                    }
                    else
                    {
                        // Ödeme alınmamış / satış iptal -> Ana menüye dön
                        _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.Navigation);
                        // Eğer sende ayrıca main menu event’i varsa, onu da publish edebilirsin:
                        // EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateNavigation);
                    }
                }
            }

            ExpectedAction = null;

            _messagingService.SendMessage(
                Messages.TicketRefreshMessage,
                result.TicketId.ToString(CultureInfo.InvariantCulture));

            _applicationStateSetter.SetApplicationLocked(false);
        }


        public string GetPrintError()
        {
            if (SelectedTicket.Orders.Any(x => x.GetValue() == 0 && x.CalculatePrice))
                return Resources.CantCompleteOperationWhenThereIsZeroPricedProduct;

            if (!SelectedTicket.IsClosed && SelectedTicket.Orders.Count > 0)
            {
                if (_applicationState.GetTicketTagGroups()
                    .Any(x => x.ForceValue && !_fastTicketViewModel.IsTaggedWith(x.Name)))
                {
                    var tg = _applicationState.GetTicketTagGroups()
                        .First(x => x.ForceValue && !_fastTicketViewModel.IsTaggedWith(x.Name));
                    return string.Format(Resources.TagCantBeEmpty_f, tg.Name);
                }
            }
            return "";
        }

        private void SaveTicketIfNew()
        {
            if ((SelectedTicket.Id == 0 ||
                 _fastTicketOrdersViewModel.Orders.Any(x => x.Model.Id == 0)) &&
                _fastTicketOrdersViewModel.Orders.Count > 0)
            {
                var result = _ticketService.CloseTicket(SelectedTicket);
                OpenTicket(result.TicketId);
            }
        }

        #endregion
    }
}
