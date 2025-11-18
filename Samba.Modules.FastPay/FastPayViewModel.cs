using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Entities;
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


/* FastPayViewModel
 * Handles the main FastPay view model in the Samba POS system.
 * Has entities for ticket management, menu item selection, and ticket orders.
 * When an entity is selected, it checks for existing tickets and opens or creates tickets as needed.
 * An entity can also be assigned to a customer within the ticket.
 */

namespace Samba.Modules.FastPayModule
{
    [Export]
    public class FastPayViewModel : ObservableObject
    {
        private readonly ITicketService _ticketService;
        private readonly ITicketServiceBase _ticketServiceBase;
        private readonly IUserService _userService;
        private readonly ICacheService _cacheService;
        private readonly IMessagingService _messagingService;
        private readonly IApplicationState _applicationState;
        private readonly IApplicationStateSetter _applicationStateSetter;
        private readonly IRegionManager _regionManager;

        private readonly FastMenuItemSelectorViewModel _fastMenuItemSelectorViewModel;
        private readonly FastTicketListViewModel _fastTicketListViewModel;
        private readonly FastTicketTagListViewModel _fastTicketTagListViewModel;
        private readonly FastTicketEntityListViewModel _fastTicketEntityListViewModel;
        private readonly FastTicketTypeListViewModel _fastTicketTypeListViewModel;
        private readonly AccountBalances _accountBalances;

        private readonly FastMenuItemSelectorView _fastMenuItemSelectorView;
        private readonly FastTicketViewModel _fastTicketViewModel;
        private readonly FastTicketOrdersViewModel _fastTicketOrdersViewModel;

        private Entity _lastSelectedEntity;
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
                    if (screenMenuId == 0) screenMenuId = _applicationState.CurrentTicketType.ScreenMenuId;
                }
                _fastMenuItemSelectorViewModel.UpdateCurrentScreenMenu(screenMenuId);
            }
        }

        [ImportingConstructor]
        public FastPayViewModel(IRegionManager regionManager, IApplicationState applicationState, IApplicationStateSetter applicationStateSetter,
            ITicketService ticketService, ITicketServiceBase ticketServiceBase, IUserService userService, ICacheService cacheService, IMessagingService messagingService,
            FastTicketListViewModel fastTicketListViewModel, FastTicketTagListViewModel fastTicketTagListViewModel, FastMenuItemSelectorViewModel fastMenuItemSelectorViewModel,
            FastMenuItemSelectorView fastMenuItemSelectorView, FastTicketViewModel fastTicketViewModel, FastTicketOrdersViewModel fastTicketOrdersViewModel,
            FastTicketEntityListViewModel fastTicketEntityListViewModel, FastTicketTypeListViewModel fastTicketTypeListViewModel, AccountBalances accountBalances)
        {
            _ticketService = ticketService;
            _ticketServiceBase = ticketServiceBase;
            _userService = userService;
            _cacheService = cacheService;
            _messagingService = messagingService;
            _applicationState = applicationState;
            _applicationStateSetter = applicationStateSetter;
            _regionManager = regionManager;

            _fastMenuItemSelectorView = fastMenuItemSelectorView;
            _fastTicketViewModel = fastTicketViewModel;
            _fastTicketOrdersViewModel = fastTicketOrdersViewModel;
            _fastMenuItemSelectorViewModel = fastMenuItemSelectorViewModel;
            _fastTicketListViewModel = fastTicketListViewModel;
            _fastTicketTagListViewModel = fastTicketTagListViewModel;
            _fastTicketEntityListViewModel = fastTicketEntityListViewModel;
            _fastTicketTypeListViewModel = fastTicketTypeListViewModel;
            _accountBalances = accountBalances;

            EventServiceFactory.EventService.GetEvent<GenericEvent<Order>>().Subscribe(OnOrderEventReceived);
            EventServiceFactory.EventService.GetEvent<GenericEvent<Ticket>>().Subscribe(OnTicketEventReceived);
            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(OnTicketEvent);
            EventServiceFactory.EventService.GetEvent<GenericEvent<ScreenMenuItemData>>().Subscribe(OnMenuItemSelected);
            EventServiceFactory.EventService.GetEvent<GenericIdEvent>().Subscribe(OnTicketIdPublished);
            EventServiceFactory.EventService.GetEvent<GenericEvent<OperationRequest<Entity>>>().Subscribe(OnEntitySelectedForTicket);
            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketTagGroup>>().Subscribe(OnTicketTagSelected);
            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketStateData>>().Subscribe(OnTicketStateSelected);
            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketType>>().Subscribe(OnTicketTypeChanged);

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(
            x =>
            {
                if (x.Topic == EventTopicNames.ResetCache && _applicationState.CurrentTicketType != null)
                {
                    _fastMenuItemSelectorViewModel.Reset();
                    _fastMenuItemSelectorViewModel.UpdateCurrentScreenMenu(_applicationState.CurrentTicketType.GetScreenMenuId(_applicationState.CurrentTerminal));
                }
            });
        }

        // Methods renamed internally to use FastPay equivalents
        private void OnOrderEventReceived(EventParameters<Order> obj)
        {
            if (obj.Topic == EventTopicNames.OrderAdded && obj.Value != null && SelectedTicket != null)
            {
                _fastTicketOrdersViewModel.SelectedTicket = SelectedTicket;
                DisplaySingleTicket();
            }
        }

        private void OnTicketTypeChanged(EventParameters<TicketType> obj)
        {
            if (obj.Topic == EventTopicNames.TicketTypeChanged && obj.Value != null)
            {
                _fastMenuItemSelectorViewModel.UpdateCurrentScreenMenu(obj.Value.GetScreenMenuId(_applicationState.CurrentTerminal));
            }

            if (obj.Topic == EventTopicNames.TicketTypeSelected && obj.Value != null)
            {
                _applicationState.TempTicketType = obj.Value;
                new OperationRequest<Entity>(_lastSelectedEntity, null).PublishEvent(EventTopicNames.EntitySelected, true);
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
                else DisplayTickets();
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
                else DisplayTickets();
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

            if (obj.Topic == EventTopicNames.MoveSelectedOrders)
            {
                var newTicketId = _ticketService.MoveOrders(SelectedTicket, SelectedTicket.ExtractSelectedOrders().ToArray(), 0).TicketId;
                SelectedTicket = null;
                OpenTicket(newTicketId);
                DisplaySingleTicket();
            }
        }

        private void OnEntitySelectedForTicket(EventParameters<OperationRequest<Entity>> eventParameters)
        {
            if (eventParameters.Topic == EventTopicNames.EntitySelected)
            {
                FireEntitySelectedRule(eventParameters.Value.SelectedItem);
                if (SelectedTicket != null)
                {
                    _ticketService.UpdateEntity(SelectedTicket, eventParameters.Value.SelectedItem);
                    if (_applicationState.CurrentDepartment != null && _applicationState.CurrentDepartment.TicketCreationMethod == 0
                        && _applicationState.SelectedEntityScreen != null
                        && SelectedTicket.Orders.Count > 0 && eventParameters.Value.SelectedItem.Id > 0
                        && _applicationState.TempEntityScreen != null
                        && eventParameters.Value.SelectedItem.EntityTypeId == _applicationState.TempEntityScreen.EntityTypeId)
                        CloseTicket();
                    else DisplaySingleTicket();
                }
                else
                {
                    var openTickets = _ticketServiceBase.GetOpenTicketIds(eventParameters.Value.SelectedItem.Id).ToList();
                    if (!openTickets.Any())
                    {
                        if (_applicationState.SelectedEntityScreen != null &&
                            _applicationState.SelectedEntityScreen.AskTicketType &&
                            _cacheService.GetTicketTypes().Count() > 1 &&
                            _applicationState.TempTicketType == null)
                        {
                            _lastSelectedEntity = eventParameters.Value.SelectedItem;
                            DisplayTicketTypeList();
                            return;
                        }

                        if (_applicationState.TempTicketType != null)
                        {
                            _applicationStateSetter.SetCurrentTicketType(_applicationState.TempTicketType);
                            _applicationState.TempTicketType = null;
                        }

                        OpenTicket(0);
                        _ticketService.UpdateEntity(SelectedTicket, eventParameters.Value.SelectedItem);
                    }
                    else if (openTickets.Count > 1)
                    {
                        _lastSelectedEntity = eventParameters.Value.SelectedItem;
                        _fastTicketListViewModel.UpdateListByEntity(eventParameters.Value.SelectedItem);
                        DisplayTicketList();
                        return;
                    }
                    else
                    {
                        OpenTicket(openTickets.ElementAt(0));
                    }
                    EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
                }
            }
        }

        private void FireEntitySelectedRule(Entity entity)
        {
            if (entity != null && entity != Entity.Null)
            {
                var entityType = _cacheService.GetEntityTypeById(entity.EntityTypeId);
                if (entityType != null)
                {
                    _applicationState.NotifyEvent(RuleEventNames.EntitySelected, new
                    {
                        Ticket = SelectedTicket,
                        EntityTypeName = entityType.Name,
                        EntityName = entity.Name,
                        EntityCustomData = entity.CustomData,
                        IsTicketSelected = SelectedTicket != null
                    });
                }
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
                    OpenTicket(0);
                    _ticketService.UpdateEntity(SelectedTicket, _lastSelectedEntity);
                }

                Debug.Assert(SelectedTicket != null);
                _fastTicketOrdersViewModel.AddOrder(obj.Value);
                DisplaySingleTicket();
            }
        }

        private void CreateTicket()
        {
            IEnumerable<TicketEntity> tr = new List<TicketEntity>();
            if (SelectedTicket != null)
            {
                tr = SelectedTicket.TicketEntities;
                CloseTicket();
                if (SelectedTicket != null) return;
            }

            OpenTicket(0);
            foreach (var ticketEntity in tr)
            {
                if (_applicationState.CurrentTicketType.EntityTypeAssignments.Any(
                        x => x.CopyToNewTickets && x.EntityTypeId == ticketEntity.EntityTypeId))
                {
                    var entity = _cacheService.GetEntityById(ticketEntity.EntityId);
                    _ticketService.UpdateEntity(SelectedTicket, entity, ticketEntity.AccountTypeId, ticketEntity.AccountId, ticketEntity.EntityCustomData);
                }
            }
        }

        private void OnTicketEvent(EventParameters<EventAggregator> obj)
        {
            switch (obj.Topic)
            {
                case EventTopicNames.CreateTicket:
                    CreateTicket();
                    EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                    break;
                case EventTopicNames.ActivatePosView:
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

        public void DisplayTickets()
        {
            _lastSelectedEntity = null;
            Debug.Assert(_applicationState.CurrentDepartment != null);
            if (SelectedTicket != null || !_applicationState.GetTicketEntityScreens().Any() || _applicationState.CurrentDepartment.TicketCreationMethod == 1)
            {
                _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);
                if (SelectedTicket == null) SelectedTicket = null;
                DisplaySingleTicket();
                return;
            }
            CommonEventPublisher.PublishEntityOperation<Entity>(null, EventTopicNames.SelectEntity, EventTopicNames.EntitySelected);
        }

        private void DisplaySingleTicket()
        {
            _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);

            if (ShouldDisplayEntityList(SelectedTicket))
            {
                _fastTicketEntityListViewModel.Update(SelectedTicket);
                DisplayTicketEntityList();
                return;
            }

            if (ShouldDisplayTicketTagList(SelectedTicket))
            {
                _fastTicketTagListViewModel.Update(SelectedTicket);
                DisplayTicketTagList();
                return;
            }

            if (SelectedTicket != null && !_userService.IsUserPermittedFor(PermissionNames.DisplayOtherWaitersTickets) && SelectedTicket.Orders.Any() && SelectedTicket.Orders[0].CreatingUserName != _applicationState.CurrentLoggedInUser.Name)
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
                && _applicationState.GetTicketTagGroups().Any(x => x.AskBeforeCreatingTicket && !ticket.IsTaggedWith(x.Name));
        }

        private bool ShouldDisplayEntityList(Ticket ticket)
        {
            return ticket != null
                && ticket.Orders.Count == 0
                && _cacheService.GetTicketTypeById(ticket.TicketTypeId).EntityTypeAssignments.Any(
                x => x.AskBeforeCreatingTicket && ticket.TicketEntities.All(y => y.EntityTypeId != x.EntityTypeId));
        }

        private void DisplayTicketTypeList()
        {
            _fastTicketTypeListViewModel.Update();
            _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);
            _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
            _regionManager.RequestNavigate(RegionNames.FastPayMainRegion, new Uri("FastTicketTypeListView", UriKind.Relative));
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

        private void DisplayTicketEntityList()
        {
            _applicationStateSetter.SetCurrentApplicationScreen(AppScreens.TicketView);
            _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
            _regionManager.RequestNavigate(RegionNames.FastPayMainRegion, new Uri("FastTicketEntityListView", UriKind.Relative));
        }

        public void DisplayMenuScreen()
        {
            _regionManager.RequestNavigate(RegionNames.MainRegion, new Uri("FastPayView", UriKind.Relative));
            _regionManager.RequestNavigate(RegionNames.FastPaySubRegion, new Uri("FastMenuItemSelectorView", UriKind.Relative));
        }

        public bool HandleTextInput(string text)
        {
            return _regionManager.Regions[RegionNames.FastPaySubRegion].ActiveViews.Contains(_fastMenuItemSelectorView)
                && _fastMenuItemSelectorViewModel.HandleTextInput(text);
        }

        public void OpenTicket(int id)
        {
            _applicationStateSetter.SetApplicationLocked(true);
            SelectedTicket = _ticketService.OpenTicket(id);
        }

        private void CloseTicket()
        {
            if (SelectedTicket == null) return;

            if (!SelectedTicket.CanCloseTicket() && !SelectedTicket.IsTaggedWithDefinedTags(_cacheService.GetTicketTagGroupNames()))
            {
                return;
            }

            if (_fastTicketOrdersViewModel.Orders.Count > 0 && SelectedTicket.GetRemainingAmount() == 0)
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
                    if (_applicationState.IsFastPayMode)
                    {
                        EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
                        _applicationState.IsFastPayMode = false;
                        _applicationState.IsPaymentDone = false;
                    }
                    else EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivatePosView);
                }
            }
            ExpectedAction = null;
            _messagingService.SendMessage(Messages.TicketRefreshMessage, result.TicketId.ToString(CultureInfo.InvariantCulture));
            _applicationStateSetter.SetApplicationLocked(false);
        }

        public string GetPrintError()
        {
            if (SelectedTicket.Orders.Any(x => x.GetValue() == 0 && x.CalculatePrice))
                return Resources.CantCompleteOperationWhenThereIsZeroPricedProduct;
            if (!SelectedTicket.IsClosed && SelectedTicket.Orders.Count > 0)
            {
                if (_applicationState.GetTicketTagGroups().Any(x => x.ForceValue && !_fastTicketViewModel.IsTaggedWith(x.Name)))
                    return string.Format(Resources.TagCantBeEmpty_f, _applicationState.GetTicketTagGroups().First(x => x.ForceValue && !_fastTicketViewModel.IsTaggedWith(x.Name)).Name);
            }
            return "";
        }

        private void SaveTicketIfNew()
        {
            if ((SelectedTicket.Id == 0 || _fastTicketOrdersViewModel.Orders.Any(x => x.Model.Id == 0)) && _fastTicketOrdersViewModel.Orders.Count > 0)
            {
                var result = _ticketService.CloseTicket(SelectedTicket);
                OpenTicket(result.TicketId);
            }
        }
    }
}
