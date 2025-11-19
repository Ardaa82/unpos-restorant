using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Commands;
using Samba.Presentation.Services;
using Samba.Presentation.Services.Common;
using Samba.Services;
using Samba.Services.Common;

namespace Samba.Modules.ModifierModule
{
    [Export]
    public class FastAutomationCommandSelectorViewModel : ObservableObject
    {
        private readonly IApplicationState _applicationState;
        private readonly IExpressionService _expressionService;

        public DelegateCommand<AutomationCommandData> SelectAutomationCommand { get; set; }
        public ICaptionCommand CloseCommand { get; set; }

        [ImportingConstructor]
        public FastAutomationCommandSelectorViewModel(IApplicationState applicationState, IExpressionService expressionService)
        {
            _applicationState = applicationState;
            _expressionService = expressionService;

            SelectAutomationCommand = new DelegateCommand<AutomationCommandData>(OnSelectAutomationCommand, CanSelectAutomationCommand);
            CloseCommand = new CaptionCommand<string>(Resources.Close, OnCloseCommandExecuted);
        }

        private Ticket _selectedTicket;
        public Ticket SelectedTicket
        {
            get { return _selectedTicket; }
            set
            {
                _selectedTicket = value;
                UpdateAutomationCommands();
            }
        }

        public IEnumerable<AutomationCommandData> AutomationCommands { get; set; }

        public int ColumnCount
        {
            get
            {
                var count = AutomationCommands != null ? AutomationCommands.Count() : 0;
                if (count == 0) return 1;
                return count % 7 == 0 ? count / 7 : (count / 7) + 1;
            }
        }

        private bool CanSelectAutomationCommand(AutomationCommandData arg)
        {
            return arg != null
                   && arg.CanExecute(SelectedTicket)
                   && _expressionService.EvalCommand(
                       FunctionNames.CanExecuteAutomationCommand,
                       arg.AutomationCommand,
                       new { Ticket = SelectedTicket },
                       true);
        }

        private void OnSelectAutomationCommand(AutomationCommandData obj)
        {
            if (obj == null) return;
            obj.PublishEvent(EventTopicNames.HandlerRequested, true);
        }

        private void OnCloseCommandExecuted(string obj)
        {
            // FastPay ekranına geri dönmek için FastPay event’ini kullanıyoruz
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
        }

        private void UpdateAutomationCommands()
        {
            AutomationCommands = _applicationState
                .GetAutomationCommands()
                .Where(x => x.DisplayOnCommandSelector && x.CanDisplay(_selectedTicket));

            RaisePropertyChanged(() => AutomationCommands);
            RaisePropertyChanged(() => ColumnCount);
        }
    }
}
