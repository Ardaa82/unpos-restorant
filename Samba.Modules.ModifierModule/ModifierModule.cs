using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Automation;
using Samba.Domain.Models.Tickets;
using Samba.Presentation.Common;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Presentation.ViewModels;
using Samba.Services.Common;

namespace Samba.Modules.ModifierModule
{
    [ModuleExport(typeof(ModifierModule))]
    class ModifierModule : ModuleBase
    {
        private IEnumerable<Order> _selectedOrders;

        private readonly IRegionManager _regionManager;
        private readonly IApplicationState _applicationState;

        // POS tarafı view / VM’ler
        private readonly OrderTagGroupEditorView _selectedOrdersView;
        private readonly OrderTagGroupEditorViewModel _selectedOrdersViewModel;
        private readonly AutomationCommandSelectorView _automationCommandSelectorView;
        private readonly AutomationCommandSelectorViewModel _automationCommandSelectorViewModel;
        private readonly AutomationCommandValueSelectorView _automationCommandValueSelectorView;
        private readonly ProductTimerEditorView _productTimerEditorView;
        private readonly ProductTimerEditorViewModel _productTimerEditorViewModel;
        private readonly TicketLogViewerView _ticketLogViewerView;
        private readonly TicketLogViewerViewModel _ticketLogViewerViewModel;
        private readonly TicketNoteEditorView _ticketNoteEditorView;
        private readonly TicketNoteEditorViewModel _ticketNoteEditorViewModel;
        private readonly TicketTagEditorView _ticketTagEditorView;
        private readonly TicketTagEditorViewModel _ticketTagEditorViewModel;

        // FastPay tarafı view / VM’ler
        private readonly FastOrderTagGroupEditorView _fastSelectedOrdersView;
        private readonly FastOrderTagGroupEditorViewModel _fastSelectedOrdersViewModel;
        private readonly FastAutomationCommandSelectorView _fastAutomationCommandSelectorView;
        private readonly FastAutomationCommandSelectorViewModel _fastAutomationCommandSelectorViewModel;
        private readonly FastAutomationCommandValueSelectorView _fastAutomationCommandValueSelectorView;
        private readonly FastProductTimerEditorView _fastProductTimerEditorView;
        private readonly FastProductTimerEditorViewModel _fastProductTimerEditorViewModel;
        private readonly FastTicketLogViewerView _fastTicketLogViewerView;
        private readonly FastTicketLogViewerViewModel _fastTicketLogViewerViewModel;
        private readonly FastTicketNoteEditorView _fastTicketNoteEditorView;
        private readonly FastTicketNoteEditorViewModel _fastTicketNoteEditorViewModel;
        private readonly FastTicketTagEditorView _fastTicketTagEditorView;
        private readonly FastTicketTagEditorViewModel _fastTicketTagEditorViewModel;

        private bool IsFastPayMode
        {
            get { return _applicationState != null && _applicationState.IsFastPayMode; }
        }

        [ImportingConstructor]
        public ModifierModule(
            IRegionManager regionManager,
            IUserService userService,
            IApplicationState applicationState,

            // POS bileşenleri
            TicketNoteEditorView ticketNoteEditorView,
            TicketNoteEditorViewModel ticketNoteEditorViewModel,
            TicketTagEditorView ticketTagEditorView,
            TicketTagEditorViewModel ticketTagEditorViewModel,
            OrderTagGroupEditorView selectedOrdersView,
            OrderTagGroupEditorViewModel selectedOrdersViewModel,
            AutomationCommandSelectorView automationCommandSelectorView,
            AutomationCommandSelectorViewModel automationCommandSelectorViewModel,
            AutomationCommandValueSelectorView automationCommandValueSelectorView,
            AutomationCommandValueSelectorViewModel automationCommandValueSelectorViewModel,
            ProductTimerEditorView productTimerEditorView,
            ProductTimerEditorViewModel productTimerEditorViewModel,
            TicketLogViewerView ticketLogViewerView,
            TicketLogViewerViewModel ticketLogViewerViewModel,

            // FastPay bileşenleri
            FastTicketNoteEditorView fastTicketNoteEditorView,
            FastTicketNoteEditorViewModel fastTicketNoteEditorViewModel,
            FastTicketTagEditorView fastTicketTagEditorView,
            FastTicketTagEditorViewModel fastTicketTagEditorViewModel,
            FastOrderTagGroupEditorView fastSelectedOrdersView,
            FastOrderTagGroupEditorViewModel fastSelectedOrdersViewModel,
            FastAutomationCommandSelectorView fastAutomationCommandSelectorView,
            FastAutomationCommandSelectorViewModel fastAutomationCommandSelectorViewModel,
            FastAutomationCommandValueSelectorView fastAutomationCommandValueSelectorView,
            FastAutomationCommandValueSelectorViewModel fastAutomationCommandValueSelectorViewModel,
            FastProductTimerEditorView fastProductTimerEditorView,
            FastProductTimerEditorViewModel fastProductTimerEditorViewModel,
            FastTicketLogViewerView fastTicketLogViewerView,
            FastTicketLogViewerViewModel fastTicketLogViewerViewModel)
        {
            _regionManager = regionManager;
            _applicationState = applicationState;

            // POS
            _selectedOrdersView = selectedOrdersView;
            _selectedOrdersViewModel = selectedOrdersViewModel;
            _automationCommandSelectorView = automationCommandSelectorView;
            _automationCommandSelectorViewModel = automationCommandSelectorViewModel;
            _automationCommandValueSelectorView = automationCommandValueSelectorView;
            _productTimerEditorView = productTimerEditorView;
            _productTimerEditorViewModel = productTimerEditorViewModel;
            _ticketLogViewerView = ticketLogViewerView;
            _ticketLogViewerViewModel = ticketLogViewerViewModel;
            _ticketNoteEditorView = ticketNoteEditorView;
            _ticketNoteEditorViewModel = ticketNoteEditorViewModel;
            _ticketTagEditorView = ticketTagEditorView;
            _ticketTagEditorViewModel = ticketTagEditorViewModel;

            // FastPay
            _fastSelectedOrdersView = fastSelectedOrdersView;
            _fastSelectedOrdersViewModel = fastSelectedOrdersViewModel;
            _fastAutomationCommandSelectorView = fastAutomationCommandSelectorView;
            _fastAutomationCommandSelectorViewModel = fastAutomationCommandSelectorViewModel;
            _fastAutomationCommandValueSelectorView = fastAutomationCommandValueSelectorView;
            _fastProductTimerEditorView = fastProductTimerEditorView;
            _fastProductTimerEditorViewModel = fastProductTimerEditorViewModel;
            _fastTicketLogViewerView = fastTicketLogViewerView;
            _fastTicketLogViewerViewModel = fastTicketLogViewerViewModel;
            _fastTicketNoteEditorView = fastTicketNoteEditorView;
            _fastTicketNoteEditorViewModel = fastTicketNoteEditorViewModel;
            _fastTicketTagEditorView = fastTicketTagEditorView;
            _fastTicketTagEditorViewModel = fastTicketTagEditorViewModel;

            EventServiceFactory.EventService.GetEvent<GenericEvent<OperationRequest<SelectedOrdersData>>>().Subscribe(OnSelectedOrdersDataEvent);
            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketTagData>>().Subscribe(OnTicketTagDataSelected);
            EventServiceFactory.EventService.GetEvent<GenericEvent<Ticket>>().Subscribe(OnTicketEvent);
            EventServiceFactory.EventService.GetEvent<GenericEvent<AutomationCommand>>().Subscribe(OnAutomationCommandEvent);
        }

        private void OnAutomationCommandEvent(EventParameters<AutomationCommand> obj)
        {
            if (obj.Topic == EventTopicNames.SelectAutomationCommandValue)
            {
                if (IsFastPayMode)
                    DisplayFastAutomationCommandValueSelector();
                else
                    DisplayAutomationCommandValueSelector();
            }
        }

        private void OnTicketTagDataSelected(EventParameters<TicketTagData> obj)
        {
            if (obj.Topic == EventTopicNames.SelectTicketTag)
            {
                if (IsFastPayMode)
                {
                    var isTagSelected = _fastTicketTagEditorViewModel.TicketTagSelected(obj.Value.Ticket, obj.Value.TicketTagGroup);
                    if (!isTagSelected) DisplayFastTicketTagEditor();
                }
                else
                {
                    var isTagSelected = _ticketTagEditorViewModel.TicketTagSelected(obj.Value.Ticket, obj.Value.TicketTagGroup);
                    if (!isTagSelected) DisplayTicketTagEditor();
                }
            }
        }

        private void OnTicketEvent(EventParameters<Ticket> obj)
        {
            if (obj.Topic == EventTopicNames.SelectAutomationCommand)
            {
                if (IsFastPayMode)
                {
                    _fastAutomationCommandSelectorViewModel.SelectedTicket = obj.Value;
                    DisplayFastAutomationCommandSelector();
                }
                else
                {
                    _automationCommandSelectorViewModel.SelectedTicket = obj.Value;
                    DisplayAutomationCommandSelector();
                }
            }

            if (obj.Topic == EventTopicNames.EditTicketNote)
            {
                if (IsFastPayMode)
                {
                    _fastTicketNoteEditorViewModel.SelectedTicket = obj.Value;
                    DisplayFastTicketNoteEditor();
                }
                else
                {
                    _ticketNoteEditorViewModel.SelectedTicket = obj.Value;
                    DisplayTicketNoteEditor();
                }
            }

            // POS log vs FastPay log
            if (obj.Topic == EventTopicNames.DisplayTicketLog)
            {
                _ticketLogViewerViewModel.SelectedTicket = obj.Value;
                DisplayTicketLogViewer();
            }

            if (obj.Topic == EventTopicNames.DisplayFastTicketLog)
            {
                _fastTicketLogViewerViewModel.SelectedTicket = obj.Value;
                DisplayFastTicketLogViewer();
            }
        }

        protected override void OnInitialization()
        {
            // POS sub region kayıtları
            _regionManager.RegisterViewWithRegion(RegionNames.PosSubRegion, typeof(OrderTagGroupEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.PosSubRegion, typeof(TicketNoteEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.PosSubRegion, typeof(TicketTagEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.PosSubRegion, typeof(ProductTimerEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.PosSubRegion, typeof(AutomationCommandSelectorView));
            _regionManager.RegisterViewWithRegion(RegionNames.PosSubRegion, typeof(AutomationCommandValueSelectorView));
            _regionManager.RegisterViewWithRegion(RegionNames.PosSubRegion, typeof(TicketLogViewerView));

            // FastPaySubRegion Fast bileşenleri – ayrı tipler, o yüzden çakışma yok
            _regionManager.RegisterViewWithRegion(RegionNames.FastPaySubRegion, typeof(FastOrderTagGroupEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.FastPaySubRegion, typeof(FastTicketNoteEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.FastPaySubRegion, typeof(FastTicketTagEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.FastPaySubRegion, typeof(FastProductTimerEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.FastPaySubRegion, typeof(FastAutomationCommandSelectorView));
            _regionManager.RegisterViewWithRegion(RegionNames.FastPaySubRegion, typeof(FastAutomationCommandValueSelectorView));
            _regionManager.RegisterViewWithRegion(RegionNames.FastPaySubRegion, typeof(FastTicketLogViewerView));
        }

        // POS ekranı için
        public void DisplayTicketDetailsScreen(OperationRequest<SelectedOrdersData> currentOperationRequest)
        {
            if (IsFastPayMode)
            {
                _fastSelectedOrdersViewModel.CurrentOperationRequest = currentOperationRequest;
                _regionManager.ActivateRegion(RegionNames.FastPaySubRegion, _fastSelectedOrdersView);
                _fastTicketNoteEditorView.TicketNote.BackgroundFocus();
            }
            else
            {
                _selectedOrdersViewModel.CurrentOperationRequest = currentOperationRequest;
                _regionManager.ActivateRegion(RegionNames.PosSubRegion, _selectedOrdersView);
                _ticketNoteEditorView.TicketNote.BackgroundFocus();
            }
        }

        public void DisplayAutomationCommandSelector()
        {
            _regionManager.ActivateRegion(RegionNames.PosSubRegion, _automationCommandSelectorView);
        }

        public void DisplayAutomationCommandValueSelector()
        {
            _regionManager.ActivateRegion(RegionNames.PosSubRegion, _automationCommandValueSelectorView);
        }

        public void DisplayTicketNoteEditor()
        {
            _regionManager.ActivateRegion(RegionNames.PosSubRegion, _ticketNoteEditorView);
        }

        public void DisplayTicketLogViewer()
        {
            _regionManager.ActivateRegion(RegionNames.PosSubRegion, _ticketLogViewerView);
        }

        public void DisplayTicketTagEditor()
        {
            _regionManager.ActivateRegion(RegionNames.PosSubRegion, _ticketTagEditorView);
        }

        private void DisplayProdcutTimerEdior(Order selectedOrder)
        {
            _productTimerEditorViewModel.Update(selectedOrder);
            _regionManager.ActivateRegion(RegionNames.PosSubRegion, _productTimerEditorView);
        }

        // FastPay için karşılık gelen ekranlar
        private void DisplayFastAutomationCommandSelector()
        {
            _regionManager.ActivateRegion(RegionNames.FastPaySubRegion, _fastAutomationCommandSelectorView);
        }

        private void DisplayFastAutomationCommandValueSelector()
        {
            _regionManager.ActivateRegion(RegionNames.FastPaySubRegion, _fastAutomationCommandValueSelectorView);
        }

        private void DisplayFastTicketNoteEditor()
        {
            _regionManager.ActivateRegion(RegionNames.FastPaySubRegion, _fastTicketNoteEditorView);
        }

        private void DisplayFastTicketLogViewer()
        {
            _regionManager.ActivateRegion(RegionNames.FastPaySubRegion, _fastTicketLogViewerView);
        }

        private void DisplayFastTicketTagEditor()
        {
            _regionManager.ActivateRegion(RegionNames.FastPaySubRegion, _fastTicketTagEditorView);
        }

        private void DisplayFastProductTimerEditor(Order selectedOrder)
        {
            _fastProductTimerEditorViewModel.Update(selectedOrder);
            _regionManager.ActivateRegion(RegionNames.FastPaySubRegion, _fastProductTimerEditorView);
        }

        private void OnSelectedOrdersDataEvent(EventParameters<OperationRequest<SelectedOrdersData>> selectedOrdersEvent)
        {
            if (selectedOrdersEvent.Topic == EventTopicNames.DisplayTicketOrderDetails)
            {
                _selectedOrders = selectedOrdersEvent.Value.SelectedItem.SelectedOrders.ToList();

                if (IsFastPayMode)
                {
                    if (_fastSelectedOrdersViewModel.ShouldDisplay(selectedOrdersEvent.Value.SelectedItem.Ticket, _selectedOrders))
                    {
                        DisplayTicketDetailsScreen(selectedOrdersEvent.Value);
                    }
                    else if (_fastProductTimerEditorViewModel.ShouldDisplay(selectedOrdersEvent.Value.SelectedItem.Ticket, _selectedOrders.ToList()))
                    {
                        DisplayFastProductTimerEditor(_selectedOrders.First());
                    }
                    else
                    {
                        EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                    }
                }
                else
                {
                    if (_selectedOrdersViewModel.ShouldDisplay(selectedOrdersEvent.Value.SelectedItem.Ticket, _selectedOrders))
                    {
                        DisplayTicketDetailsScreen(selectedOrdersEvent.Value);
                    }
                    else if (_productTimerEditorViewModel.ShouldDisplay(selectedOrdersEvent.Value.SelectedItem.Ticket, _selectedOrders.ToList()))
                    {
                        DisplayProdcutTimerEdior(_selectedOrders.First());
                    }
                    else
                    {
                        EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                    }
                }
            }
        }
    }
}
