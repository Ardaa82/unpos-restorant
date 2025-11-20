using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Samba.Domain.Models.Automation;
using Samba.Domain.Models.Entities;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Modules.PosModule; // SelectedOrdersData, OrderViewModel vb. için
using Samba.Presentation.Common;
using Samba.Presentation.Common.Commands;
using Samba.Presentation.Common.Services;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Presentation.ViewModels;
using Samba.Services;
using Samba.Services.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Samba.Modules.FastPayModule;          // FastTicketOrdersViewModel, FastEntityButton, FastCommandContainerButton, FastTicketInfoViewModel
using FastPay.Presentation.ViewModels;      // FastTicketTotalsViewModel

namespace Samba.Modules.FastPay
{
    [Export]
    public class FastTicketViewModel : ObservableObject
    {
        private readonly ITicketService _ticketService;
        private readonly IUserService _userService;
        private readonly ICacheService _cacheService;
        private readonly IApplicationState _applicationState;
        private readonly IExpressionService _expressionService;
        private readonly FastTicketOrdersViewModel _ticketOrdersViewModel;
        private readonly FastTicketTotalsViewModel _totals;
        private readonly FastTicketInfoViewModel _ticketInfo;
        // FastPay için tek slotluk bekletilmiş bilet ID'si
        private static int _heldTicketId;
        public CaptionCommand<string> MoveOrdersCommand { get; set; }
        public ICaptionCommand IncQuantityCommand { get; set; }
        public ICaptionCommand DecQuantityCommand { get; set; }
        public ICaptionCommand IncSelectionQuantityCommand { get; set; }
        public ICaptionCommand DecSelectionQuantityCommand { get; set; }
        public ICaptionCommand ShowTicketTagsCommand { get; set; }
        public ICaptionCommand CancelItemCommand { get; set; }
        public ICaptionCommand EditTicketNoteCommand { get; set; }
        public ICaptionCommand RemoveTicketLockCommand { get; set; }
        public ICaptionCommand ChangePriceCommand { get; set; }
        public ICaptionCommand AddOrderCommand { get; set; }
        public ICaptionCommand ModifyOrderCommand { get; set; }
        public ICaptionCommand ClearTicketContentsCommand { get; set; } // Belgeyi Kapat butonu
        public ICaptionCommand DiscountCommand { get; set; } // İskonto butonu

        public ICaptionCommand HoldTicketCommand { get; set; } // Belge Beklet / Belge Çağır

        public DelegateCommand<EntityType> SelectEntityCommand { get; set; }
        public DelegateCommand<FastCommandContainerButton> ExecuteAutomationCommnand { get; set; }

        private ObservableCollection<FastEntityButton> _entityButtons;
        public ObservableCollection<FastEntityButton> EntityButtons
        {
            get
            {
                if (_entityButtons == null && SelectedDepartment != null && SelectedTicket != null && SelectedTicket.TicketTypeId > 0)
                {
                    _entityButtons = new ObservableCollection<FastEntityButton>(
                        _cacheService.GetEntityTypesByTicketType(SelectedTicket.TicketTypeId)
                            .Select(x => new FastEntityButton(x, SelectedTicket)));
                }
                else if (_entityButtons == null && _applicationState.CurrentTicketType != null && _applicationState.CurrentTicketType.Id > 0)
                {
                    _entityButtons = new ObservableCollection<FastEntityButton>(
                        _cacheService.GetEntityTypesByTicketType(_applicationState.CurrentTicketType.Id)
                            .Select(x => new FastEntityButton(x, SelectedTicket)));
                }
                return _entityButtons;
            }
        }

        private Ticket _selectedTicket;
        public Ticket SelectedTicket
        {
            get { return _selectedTicket; }
            set
            {
                _entityButtons = null;
                _allAutomationCommands = null;
                _selectedTicket = value ?? Ticket.Empty;
                _totals.Model = _selectedTicket;
                _ticketOrdersViewModel.SelectedTicket = _selectedTicket;
                _ticketInfo.SelectedTicket = _selectedTicket;
                RaisePropertyChanged(() => EntityButtons);
                RaisePropertyChanged(() => TicketAutomationCommands);
            }
        }

        public FastTicketInfoViewModel TicketInfo { get { return _ticketInfo; } }
        public IEnumerable<Order> SelectedOrders { get { return SelectedTicket.SelectedOrders; } }
        public Order SelectedOrder => _ticketOrdersViewModel != null && SelectedOrders.Count() == 1 ? SelectedOrders.ElementAt(0) : null;
        public Department SelectedDepartment => _applicationState.CurrentDepartment != null ? _applicationState.CurrentDepartment.Model : null;

        public bool IsPortrait => !_applicationState.IsLandscape;
        public bool IsLandscape => _applicationState.IsLandscape;
        public bool IsAddOrderButtonVisible => IsPortrait && IsNothingSelected;
        public bool IsModifyOrderButtonVisible => IsPortrait && IsItemsSelected;
        public bool IsItemsSelected => SelectedOrders.Any();
        public bool IsItemsSelectedAndUnlocked => SelectedOrders.Any() && SelectedOrders.Count(x => x.Locked) == 0;
        public bool IsItemsSelectedAndLocked => SelectedOrders.Any() && SelectedOrders.Count(x => !x.Locked) == 0;
        public bool IsNothingSelected => !SelectedOrders.Any();
        public bool IsNothingSelectedAndTicketLocked => !SelectedOrders.Any() && SelectedTicket.IsLocked;
        public bool IsNothingSelectedAndTicketTagged => !SelectedOrders.Any() && SelectedTicket.IsTagged;
        public bool IsTicketSelected => SelectedTicket != Ticket.Empty;

        public bool HasHeldTicket => _heldTicketId > 0;

        // Butonun metni: slot boşken Beklet, doluyken Çağır
        public string HoldTicketCaption => HasHeldTicket ? "Belge Çağır" : "Belge Beklet";

        // Butonun görünürlüğü: ya aktif bilet varsa ya da bekletilmiş bilet varsa
        public bool IsHoldButtonVisible => HasHeldTicket || IsTicketSelected;


        public OrderViewModel LastSelectedOrder { get; set; }
        public bool ClearSelection { get; set; }

        private IEnumerable<FastCommandContainerButton> _allAutomationCommands;
        private IEnumerable<FastCommandContainerButton> AllAutomationCommands
        {
            get
            {
                return _allAutomationCommands ??
                       (_allAutomationCommands =
                           _applicationState.GetAutomationCommands()
                                .Select(x => new FastCommandContainerButton(x, SelectedTicket))
                                .ToList());
            }
        }

        public IEnumerable<FastCommandContainerButton> TicketAutomationCommands
        {
            get
            {
                return AllAutomationCommands.Where(x =>
                    x.CommandContainer.DisplayOnTicket &&
                    x.CommandContainer.CanDisplay(SelectedTicket));
            }
        }

        public IEnumerable<FastCommandContainerButton> OrderAutomationCommands
        {
            get
            {
                return AllAutomationCommands.Where(x =>
                    x.CommandContainer.DisplayOnOrders &&
                    x.CommandContainer.CanDisplay(SelectedTicket));
            }
        }

        public IEnumerable<FastCommandContainerButton> UnderTicketAutomationCommands
        {
            get
            {
                return AllAutomationCommands.Where(x =>
                    x.CommandContainer.DisplayUnderTicket &&
                    x.CommandContainer.CanDisplay(SelectedTicket));
            }
        }

        public IEnumerable<FastCommandContainerButton> UnderTicketRow2AutomationCommands
        {
            get
            {
                return AllAutomationCommands.Where(x =>
                    x.CommandContainer.DisplayUnderTicket2 &&
                    x.CommandContainer.CanDisplay(SelectedTicket));
            }
        }

        public IEnumerable<FastTicketTagButton> TicketTagButtons
        {
            get
            {
                return _applicationState.CurrentDepartment != null
                    ? _applicationState.GetTicketTagGroups()
                        .OrderBy(x => x.SortOrder)
                        .Select(x => new FastTicketTagButton(x, SelectedTicket))
                    : null;
            }
        }

        [ImportingConstructor]
        public FastTicketViewModel(IApplicationState applicationState, IExpressionService expressionService,
            ITicketService ticketService, IAccountService accountService, IEntityServiceClient locationService,
            IUserService userService, ICacheService cacheService,
            FastTicketOrdersViewModel ticketOrdersViewModel,
            FastTicketTotalsViewModel totals,
            FastTicketInfoViewModel ticketInfoViewModel)
        {
            _ticketService = ticketService;
            _userService = userService;
            _cacheService = cacheService;
            _applicationState = applicationState;
            _expressionService = expressionService;
            _ticketOrdersViewModel = ticketOrdersViewModel;
            _totals = totals;
            _ticketInfo = ticketInfoViewModel;

            SelectEntityCommand = new DelegateCommand<EntityType>(OnSelectEntity, CanSelectEntity);
            ExecuteAutomationCommnand = new DelegateCommand<FastCommandContainerButton>(OnExecuteAutomationCommand, CanExecuteAutomationCommand);

            IncQuantityCommand = new CaptionCommand<string>("+", OnIncQuantityCommand, CanIncQuantity);
            DecQuantityCommand = new CaptionCommand<string>("-", OnDecQuantityCommand, CanDecQuantity);
            IncSelectionQuantityCommand = new CaptionCommand<string>("(+)", OnIncSelectionQuantityCommand, CanIncSelectionQuantity);
            DecSelectionQuantityCommand = new CaptionCommand<string>("(-)", OnDecSelectionQuantityCommand, CanDecSelectionQuantity);
            ShowTicketTagsCommand = new CaptionCommand<TicketTagGroup>(Resources.Tag, OnShowTicketsTagExecute, CanExecuteShowTicketTags);
            CancelItemCommand = new CaptionCommand<string>(Resources.Cancel, OnCancelItemCommand);
            MoveOrdersCommand = new CaptionCommand<string>(Resources.MoveTicketLine, OnMoveOrders, CanMoveOrders);
            EditTicketNoteCommand = new CaptionCommand<string>(Resources.TicketNote.Replace(" ", Environment.NewLine), OnEditTicketNote, CanEditTicketNote);
            RemoveTicketLockCommand = new CaptionCommand<string>(Resources.ReleaseLock, OnRemoveTicketLock, CanRemoveTicketLock);
            ChangePriceCommand = new CaptionCommand<string>(Resources.ChangePrice, OnChangePrice, CanChangePrice);
            AddOrderCommand = new CaptionCommand<string>(Resources.AddOrder.Replace(" ", Environment.NewLine), OnAddOrder, CanAddOrder);
            ModifyOrderCommand = new CaptionCommand<string>(Resources.ModifyOrder.Replace(" ", Environment.NewLine), OnModifyOrder, CanModifyOrder);
            ClearTicketContentsCommand = new CaptionCommand<string>("Belgeyi Kapat", OnClearTicketContents, CanClearTicketContents);
            DiscountCommand = new CaptionCommand<string>("İskonto", OnApplyDiscount, CanApplyDiscount);
            HoldTicketCommand = new CaptionCommand<string>("Belge Beklet", OnHoldOrRecallTicket, CanHoldOrRecallTicket);

            EventServiceFactory.EventService.GetEvent<GenericEvent<OrderViewModel>>().Subscribe(OnSelectedOrdersChanged);
            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(OnRefreshTicket);
            EventServiceFactory.EventService.GetEvent<GenericEvent<OrderTagData>>().Subscribe(OnOrderTagEvent);
            EventServiceFactory.EventService.GetEvent<GenericEvent<MenuItemPortion>>().Subscribe(OnPortionSelected);
            EventServiceFactory.EventService.GetEvent<GenericEvent<Department>>().Subscribe(OnDepartmentChanged);
            EventServiceFactory.EventService.GetEvent<GenericEvent<AutomationCommandValueData>>().Subscribe(OnAutomationCommandValueSelected);
            EventServiceFactory.EventService.GetEvent<GenericEvent<AutomationCommandData>>().Subscribe(OnAutomationCommandSelected);

            SelectedTicket = Ticket.Empty;
        }
        private bool CanHoldOrRecallTicket(string arg)
        {
            if (!_applicationState.IsFastPayMode) return false;

            // Slot boşsa: bekletmek için geçerli bir ticket lazım
            if (!HasHeldTicket)
            {
                return SelectedTicket != null
                       && SelectedTicket != Ticket.Empty
                       && !SelectedTicket.IsClosed;
            }

            // Slot doluyken: her durumda çağırabilsin
            return HasHeldTicket;
        }

        private void OnHoldOrRecallTicket(string obj)
        {
            // Slot boş -> BEKLET
            if (!HasHeldTicket)
            {
                if (SelectedTicket == null || SelectedTicket == Ticket.Empty)
                {
                    InteractionService.UserIntraction.GiveFeedback("Bekletilecek bir belge yok.");
                    return;
                }

                if (SelectedTicket.Id <= 0)
                {
                    InteractionService.UserIntraction.GiveFeedback("Bu belgenin henüz numarası yok, önce kaydedin.");
                    return;
                }

                _heldTicketId = SelectedTicket.Id;
                InteractionService.UserIntraction.GiveFeedback("Belge bekletildi.");

                RaisePropertyChanged(() => HoldTicketCaption);
                RaisePropertyChanged(() => IsHoldButtonVisible);
                (HoldTicketCommand as CaptionCommand<string>)?.RaiseCanExecuteChanged();
                return;
            }

            // Slot dolu -> ÇAĞIR
            var ticket = _ticketService.OpenTicket(_heldTicketId);
            if (ticket == null || ticket == Ticket.Empty)
            {
                InteractionService.UserIntraction.GiveFeedback("Bekletilen belge bulunamadı.");
                _heldTicketId = 0;
                RaisePropertyChanged(() => HoldTicketCaption);
                RaisePropertyChanged(() => IsHoldButtonVisible);
                (HoldTicketCommand as CaptionCommand<string>)?.RaiseCanExecuteChanged();
                return;
            }

            SelectedTicket = ticket;
            RefreshSelectedTicketTitle();
            RefreshVisuals();

            _heldTicketId = 0;
            RaisePropertyChanged(() => HoldTicketCaption);
            RaisePropertyChanged(() => IsHoldButtonVisible);
            (HoldTicketCommand as CaptionCommand<string>)?.RaiseCanExecuteChanged();
        }




        private bool CanModifyOrder(string arg)
        {
            return SelectedOrders.Any();
        }

        private void OnModifyOrder(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            var so = new SelectedOrdersData { SelectedOrders = SelectedOrders, Ticket = SelectedTicket };
            OperationRequest<SelectedOrdersData>.Publish(
                so,
                EventTopicNames.DisplayTicketOrderDetails,
                EventTopicNames.ActivateFastPayView,
                "");
        }

        private bool CanAddOrder(string arg)
        {
            return !SelectedTicket.IsClosed && !SelectedTicket.IsLocked;
        }

        private void OnAddOrder(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateMenuView);
        }

        private bool CanExecuteAutomationCommand(FastCommandContainerButton arg)
        {
            return arg.IsEnabled
                   && arg.CommandContainer.CanExecute(SelectedTicket)
                   && _expressionService.EvalCommand(
                       FunctionNames.CanExecuteAutomationCommand,
                       arg.CommandContainer.AutomationCommand,
                       new { Ticket = SelectedTicket },
                       true);
        }

        private void OnExecuteAutomationCommand(FastCommandContainerButton obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            ExecuteAutomationCommand(obj.CommandContainer.AutomationCommand, obj.SelectedValue, obj.GetNextValue());
            obj.NextValue();
        }

        private void ExecuteAutomationCommand(AutomationCommand automationCommand, string selectedValue, string nextValue)
        {
            if (!string.IsNullOrEmpty(automationCommand.Values) && !automationCommand.ToggleValues)
                automationCommand.PublishEvent(EventTopicNames.SelectAutomationCommandValue);
            else
            {
                ExecuteAutomationCommand(automationCommand.Name, selectedValue, nextValue);
                RefreshVisuals();
            }
        }

        private void OnAutomationCommandSelected(EventParameters<AutomationCommandData> obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            if (obj.Topic == EventTopicNames.HandlerRequested)
            {
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                ExecuteAutomationCommand(obj.Value.AutomationCommand, "", "");
            }
        }

        private void OnAutomationCommandValueSelected(EventParameters<AutomationCommandValueData> obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            ExecuteAutomationCommand(obj.Value.AutomationCommand.Name, obj.Value.Value, "");
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
        }

        private void ExecuteAutomationCommand(string automationCommandName, string automationCommandValue, string nextCommandValue)
        {
            if (SelectedOrders.Any())
            {
                foreach (var selectedOrder in SelectedOrders.ToList())
                {
                    _applicationState.NotifyEvent(
                        RuleEventNames.AutomationCommandExecuted,
                        new
                        {
                            Ticket = SelectedTicket,
                            Order = selectedOrder,
                            AutomationCommandName = automationCommandName,
                            CommandValue = automationCommandValue,
                            NextCommandValue = nextCommandValue
                        });
                }
            }
            else
            {
                _applicationState.NotifyEvent(
                    RuleEventNames.AutomationCommandExecuted,
                    new
                    {
                        Ticket = SelectedTicket,
                        AutomationCommandName = automationCommandName,
                        CommandValue = automationCommandValue,
                        NextCommandValue = nextCommandValue
                    });
            }
            _ticketOrdersViewModel.SelectedTicket = SelectedTicket;
            ClearSelectedItems();
            ClearSelection = true;
        }
        private bool CanClearTicketContents(string arg)
        {
            if (!_applicationState.IsFastPayMode) return false;

            if (SelectedTicket == null || SelectedTicket == Ticket.Empty)
                return false;

            if (SelectedTicket.IsClosed || SelectedTicket.IsLocked)
                return false;

            return SelectedTicket.Orders.Any();
        }

        private void OnClearTicketContents(string obj)
        {
            if (!CanClearTicketContents(obj)) return;

            // Kullanıcıdan onay al
            if (!InteractionService.UserIntraction
                    .AskQuestion("Belgedeki tüm satırlar silinecek. Devam edilsin mi?"))
                return;

            // 1) Bilet içindeki tüm siparişleri KALICI olarak temizle
            SelectedTicket.Orders.Clear();

            // 2) Tutarları yeniden hesapla
            _ticketService.RecalculateTicket(SelectedTicket);

            // 3) Orders viewmodel’e güncel bileti ver ve görünümü yenile
            _ticketOrdersViewModel.SelectedTicket = SelectedTicket;
            _ticketOrdersViewModel.RefreshSelectedOrders();

            // 4) Üst başlık + butonlar vs. için genel refresh
            RefreshVisuals();
            ClearSelectedItems();
        }

        private bool CanApplyDiscount(string arg)
        {
            if (!_applicationState.IsFastPayMode) return false;

            if (SelectedTicket == null || SelectedTicket == Ticket.Empty)
                return false;

            if (SelectedTicket.IsClosed || SelectedTicket.IsLocked)
                return false;

            return SelectedTicket.Orders.Any();
        }

        private void OnApplyDiscount(string obj)
        {
            if (!CanApplyDiscount(obj)) return;

            // 1) Kullanıcının girdiği iskonto değeri (numaratör)
            decimal value;
            if (!decimal.TryParse(_applicationState.NumberPadValue, out value) || value <= 0)
            {
                InteractionService.UserIntraction.GiveFeedback("İskonto için geçerli bir değer giriniz.");
                return;
            }

            // 2) Yüzde mi, tutar mı?
            var isPercent = InteractionService.UserIntraction
                .AskQuestion("İskonto yüzdelik olarak uygulansın mı? (Evet: %, Hayır: tutar)");

            // 3) Hedef grup: seçili satırlar varsa onlar, yoksa tüm bilet
            var targetOrders = SelectedOrders.Any()
                ? SelectedOrders.ToList()
                : SelectedTicket.Orders.ToList();

            ApplyDiscountToOrders(targetOrders, value, isPercent);

            // 4) Bilet ve ekranı güncelle
            _ticketService.RecalculateTicket(SelectedTicket);
            _ticketOrdersViewModel.SelectedTicket = SelectedTicket;
            _ticketOrdersViewModel.RefreshSelectedOrders();
            RefreshVisuals();
            ClearSelectedItems();
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ResetNumerator);
        }

        private void ApplyDiscountToOrders(IList<Order> orders, decimal value, bool isPercent)
        {
            if (orders == null || orders.Count == 0) return;

            if (isPercent)
            {
                // Yüzde iskonto
                var rate = value / 100m;
                if (rate <= 0) return;
                if (rate > 1) rate = 1;

                foreach (var order in orders)
                {
                    var basePrice = order.Price;
                    var newPrice = basePrice * (1 - rate);
                    if (newPrice < 0) newPrice = 0;

                    order.UpdatePrice(newPrice, SelectedDepartment.PriceTag);
                }
            }
            else
            {
                // Tutar iskonto: girilen değer, hedef grubun toplamına göre paylaştırılır
                var total = orders.Sum(x => x.Price);
                if (total <= 0) return;

                foreach (var order in orders)
                {
                    var basePrice = order.Price;
                    var proportion = total == 0 ? 0 : (basePrice / total);
                    var discountForOrder = value * proportion;
                    var newPrice = basePrice - discountForOrder;
                    if (newPrice < 0) newPrice = 0;

                    order.UpdatePrice(newPrice, SelectedDepartment.PriceTag);
                }
            }
        }


        private void OnDepartmentChanged(EventParameters<Department> obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            _entityButtons = null;
            RaisePropertyChanged(() => EntityButtons);
        }

        private void ClearSelectedItems()
        {
            _ticketOrdersViewModel.ClearSelectedOrders();
            RefreshSelectedItems();
        }

        private bool CanSelectEntity(EntityType arg)
        {
            Debug.Assert(SelectedTicket != null);
            return arg != null
                   && !SelectedTicket.IsLocked
                   && SelectedTicket.CanSubmit
                   && _applicationState.GetTicketEntityScreens().Any(x => x.EntityTypeId == arg.Id);
        }

        private void OnSelectEntity(EntityType obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            var ticketEntity = SelectedTicket.TicketEntities.SingleOrDefault(x => x.EntityTypeId == obj.Id);
            var selectedEntity = ticketEntity != null
                ? _cacheService.GetEntityById(ticketEntity.EntityId)
                : Entity.GetNullEntity(obj.Id);

            OperationRequest<Entity>.Publish(
                selectedEntity,
                EventTopicNames.SelectEntity,
                EventTopicNames.EntitySelected,
                "");
        }

        private void OnPortionSelected(EventParameters<MenuItemPortion> obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            if (obj.Topic == EventTopicNames.PortionSelected)
            {
                var taxTemplate = _applicationState.GetTaxTemplates(obj.Value.MenuItemId);
                SelectedOrder.UpdatePortion(
                    obj.Value,
                    _applicationState.CurrentDepartment.PriceTag,
                    taxTemplate);

                _ticketOrdersViewModel.SelectedTicket = SelectedTicket;
                RefreshVisuals();
            }
        }

        private void OnOrderTagEvent(EventParameters<OrderTagData> obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            if (obj.Topic == EventTopicNames.OrderTagSelected)
            {
                _ticketService.TagOrders(
                    SelectedTicket,
                    SelectedTicket.ExtractSelectedOrders(),
                    obj.Value.OrderTagGroup,
                    obj.Value.SelectedOrderTag,
                    "");
                _ticketOrdersViewModel.SelectedTicket = SelectedTicket;
                ClearSelection = true;
                RefreshVisuals();
            }

            if (obj.Topic == EventTopicNames.OrderTagRemoved)
            {
                _ticketService.UntagOrders(
                    SelectedTicket,
                    SelectedTicket.ExtractSelectedOrders(),
                    obj.Value.OrderTagGroup,
                    obj.Value.SelectedOrderTag);
                _ticketOrdersViewModel.RefreshSelectedOrders();
                RefreshVisuals();
            }
        }

        private void OnRefreshTicket(EventParameters<EventAggregator> obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            if (obj.Topic == EventTopicNames.UnlockTicketRequested)
            {
                OnRemoveTicketLock("");
            }

            if (obj.Topic == EventTopicNames.RefreshSelectedTicket)
            {
                RefreshVisuals();
            }
        }

        private void OnSelectedOrdersChanged(EventParameters<OrderViewModel> obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            if (obj.Topic == EventTopicNames.SelectedOrdersChanged)
            {
                if (!obj.Value.Selected && !_ticketService.CanDeselectOrder(obj.Value.Model))
                {
                    obj.Value.ToggleSelection();
                    return;
                }

                if (ClearSelection)
                {
                    ClearSelection = false;
                    if (obj.Value != LastSelectedOrder)
                    {
                        ClearSelectedItems();
                        obj.Value.ToggleSelection();
                        return;
                    }
                }

                LastSelectedOrder = obj.Value.Selected ? obj.Value : null;
                if (!SelectedOrders.Any()) LastSelectedOrder = null;
                _ticketOrdersViewModel.UpdateLastSelectedOrder(LastSelectedOrder);

                RefreshSelectedItems();

                if (_applicationState.IsLandscape)
                {
                    var so = new SelectedOrdersData { SelectedOrders = SelectedOrders, Ticket = SelectedTicket };
                    OperationRequest<SelectedOrdersData>.Publish(
                        so,
                        EventTopicNames.DisplayTicketOrderDetails,
                        EventTopicNames.ActivateFastPayView,
                        "");
                }
            }
        }

        private bool CanExecuteShowTicketTags(TicketTagGroup arg)
        {
            return SelectedTicket.CanSubmit;
        }

        private void OnShowTicketsTagExecute(TicketTagGroup tagGroup)
        {
            if (SelectedTicket == Ticket.Empty)
            {
                tagGroup.PublishEvent(EventTopicNames.ActivateTicketList);
                return;
            }

            var ticketTagData = new TicketTagData
            {
                TicketTagGroup = tagGroup,
                Ticket = SelectedTicket
            };
            ticketTagData.PublishEvent(EventTopicNames.SelectTicketTag);
        }

        private bool CanChangePrice(string arg)
        {
            return !SelectedTicket.IsLocked
                   && SelectedTicket.CanSubmit
                   && SelectedOrder != null
                   && (SelectedOrder.Price == 0 ||
                       _userService.IsUserPermittedFor(PermissionNames.ChangeItemPrice));
        }

        private void OnChangePrice(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            decimal price;
            decimal.TryParse(_applicationState.NumberPadValue, out price);
            if (price <= 0)
            {
                InteractionService.UserIntraction.GiveFeedback(Resources.ForChangingPriceTypeAPrice);
            }
            else
            {
                SelectedOrder.UpdatePrice(price, SelectedDepartment.PriceTag);
            }
            ClearSelectedItems();
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ResetNumerator);
        }

        private bool CanRemoveTicketLock(string arg)
        {
            return SelectedTicket.IsLocked &&
                   _userService.IsUserPermittedFor(PermissionNames.AddItemsToLockedTickets);
        }

        private void OnRemoveTicketLock(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            SelectedTicket.UnLock();
            _ticketOrdersViewModel.Refresh();
            _allAutomationCommands = null;
            _entityButtons = null;
            RaisePropertyChanged(() => EntityButtons);
            RefreshVisuals();
        }

        private void OnMoveOrders(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            SelectedTicket.PublishEvent(EventTopicNames.MoveSelectedOrders);
        }

        private bool CanMoveOrders(string arg)
        {
            if (SelectedTicket.IsLocked || SelectedTicket.IsClosed) return false;
            if (!SelectedTicket.CanRemoveSelectedOrders(SelectedOrders)) return false;
            if (SelectedOrders.Any(x => x.Id == 0)) return false;
            if (SelectedOrders.Any(x => !x.Locked) &&
                _userService.IsUserPermittedFor(PermissionNames.MoveUnlockedOrders)) return true;

            return _userService.IsUserPermittedFor(PermissionNames.MoveOrders);
        }

        private bool CanEditTicketNote(string arg)
        {
            return SelectedTicket != Ticket.Empty && !SelectedTicket.IsClosed;
        }

        private void OnEditTicketNote(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            SelectedTicket.PublishEvent(EventTopicNames.EditTicketNote);
        }

        private void OnDecQuantityCommand(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            LastSelectedOrder.Quantity--;
            RefreshSelectedOrders();
        }

        private void OnIncQuantityCommand(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            LastSelectedOrder.Quantity++;
            RefreshSelectedOrders();
        }

        private bool CanDecQuantity(string arg)
        {
            return LastSelectedOrder != null &&
                   LastSelectedOrder.Quantity > 1 &&
                   !LastSelectedOrder.IsLocked;
        }

        private bool CanIncQuantity(string arg)
        {
            return LastSelectedOrder != null &&
                   !LastSelectedOrder.IsLocked;
        }

        private bool CanDecSelectionQuantity(string arg)
        {
            return LastSelectedOrder != null &&
                   LastSelectedOrder.Quantity > 1 &&
                   LastSelectedOrder.IsLocked;
        }

        private void OnDecSelectionQuantityCommand(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            LastSelectedOrder.DecSelectedQuantity();
            RefreshSelectedOrders();
        }

        private bool CanIncSelectionQuantity(string arg)
        {
            return LastSelectedOrder != null &&
                   LastSelectedOrder.Quantity > 1 &&
                   LastSelectedOrder.IsLocked;
        }

        private void OnIncSelectionQuantityCommand(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            LastSelectedOrder.IncSelectedQuantity();
            RefreshSelectedOrders();
        }

        private void OnCancelItemCommand(string obj)
        {
            if (!_applicationState.IsFastPayMode) return;

            if (!_ticketOrdersViewModel.CanCancelSelectedOrders())
            {
                ClearSelectedItems();
                return;
            }

            _ticketService.CancelSelectedOrders(SelectedTicket);
            _ticketOrdersViewModel.CancelSelectedOrders();
            _ticketService.RecalculateTicket(SelectedTicket);
            RefreshSelectedItems();
            RefreshSelectedTicket();
        }

        private string _selectedTicketTitle;
        public string SelectedTicketTitle
        {
            get { return _selectedTicketTitle; }
            set { _selectedTicketTitle = value; RaisePropertyChanged(() => SelectedTicketTitle); }
        }

        public void UpdateSelectedTicketTitle()
        {
            _totals.Model = SelectedTicket;
            var result = _totals.TitleWithAccountBalancesAndState;
            SelectedTicketTitle = result.Trim() == "#" ? Resources.NewTicket : result;
        }

        public bool IsTaggedWith(string tagGroup)
        {
            return !string.IsNullOrEmpty(SelectedTicket.GetTagValue(tagGroup));
        }

        public void ResetTicket()
        {
            RefreshVisuals();
            _totals.ResetCache();
            RefreshSelectedTicketTitle();
            ClearSelectedItems();
        }

        public void RefreshSelectedTicket()
        {
            _totals.Refresh();
            RaisePropertyChanged(() => IsTicketSelected);
            RaisePropertyChanged(() => HoldTicketCaption);
            RaisePropertyChanged(() => IsHoldButtonVisible);
            ExecuteAutomationCommnand.RaiseCanExecuteChanged();
            (HoldTicketCommand as CaptionCommand<string>)?.RaiseCanExecuteChanged();
        }


        public void RefreshSelectedTicketTitle()
        {
            _ticketInfo.Refresh();
            UpdateSelectedTicketTitle();
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            RefreshSelectedTicket();
            RaisePropertyChanged(() => IsNothingSelectedAndTicketLocked);
            RaisePropertyChanged(() => IsNothingSelectedAndTicketTagged);
            RaisePropertyChanged(() => TicketTagButtons);
            RaisePropertyChanged(() => TicketAutomationCommands);
            RaisePropertyChanged(() => UnderTicketAutomationCommands);
            RaisePropertyChanged(() => UnderTicketRow2AutomationCommands);
        }

        public void RefreshSelectedItems()
        {
            RaisePropertyChanged(() => IsItemsSelected);
            RaisePropertyChanged(() => IsNothingSelected);
            RaisePropertyChanged(() => IsNothingSelectedAndTicketLocked);
            RaisePropertyChanged(() => IsItemsSelectedAndUnlocked);
            RaisePropertyChanged(() => IsItemsSelectedAndLocked);
            RaisePropertyChanged(() => IsTicketSelected);
            RaisePropertyChanged(() => OrderAutomationCommands);
            RaisePropertyChanged(() => IsAddOrderButtonVisible);
            RaisePropertyChanged(() => IsModifyOrderButtonVisible);
        }

        public void RefreshLayout()
        {
            RaisePropertyChanged(() => IsPortrait);
            RaisePropertyChanged(() => IsLandscape);
            RaisePropertyChanged(() => IsAddOrderButtonVisible);
            RaisePropertyChanged(() => IsModifyOrderButtonVisible);
        }

        private void RefreshSelectedOrders()
        {
            _ticketOrdersViewModel.RefreshSelectedOrders();
            RefreshVisuals();
        }
    }
}
