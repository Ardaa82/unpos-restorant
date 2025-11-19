using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain.Models.Automation;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Commands;
using Samba.Presentation.Services.Common;
using Samba.Presentation.ViewModels;
using Samba.Services.Common;

namespace Samba.Modules.ModifierModule
{
    [Export]
    public class FastAutomationCommandValueSelectorViewModel : ObservableObject
    {
        [ImportingConstructor]
        public FastAutomationCommandValueSelectorViewModel()
        {
            CloseCommand = new CaptionCommand<string>(Resources.Close, OnCloseCommandExecuted);
            AutomationCommandSelectedCommand = new DelegateCommand<string>(OnAutomationCommandValueSelected);
            CommandValues = new ObservableCollection<string>();
            EventServiceFactory.EventService
                .GetEvent<GenericEvent<AutomationCommand>>()
                .Subscribe(OnAutomationCommandEvent);
        }

        private void OnAutomationCommandEvent(EventParameters<AutomationCommand> obj)
        {
            if (obj.Topic == EventTopicNames.SelectAutomationCommandValue)
            {
                CommandValues.Clear();
                SetSelectedAutomationCommand(obj.Value);

                if (!string.IsNullOrEmpty(obj.Value.Values))
                    CommandValues.AddRange(obj.Value.Values.Split('|'));

                if (CommandValues.Count == 1)
                {
                    OnAutomationCommandValueSelected(CommandValues.ElementAt(0));
                    return;
                }

                RaisePropertyChanged(() => ColumnCount);
            }
        }

        public AutomationCommand SelectedAutomationCommand { get; private set; }
        public ICaptionCommand CloseCommand { get; set; }
        public DelegateCommand<string> AutomationCommandSelectedCommand { get; set; }

        public ObservableCollection<string> CommandValues { get; set; }

        public int ColumnCount
        {
            get
            {
                var count = CommandValues != null ? CommandValues.Count : 0;
                if (count == 0) return 1;
                return count % 7 == 0 ? count / 7 : (count / 7) + 1;
            }
        }

        private static void OnCloseCommandExecuted(string obj)
        {
            // FastPay ekranına dön
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateFastPayView);
        }

        private void OnAutomationCommandValueSelected(string commandValue)
        {
            if (SelectedAutomationCommand == null || string.IsNullOrEmpty(commandValue)) return;

            var automationCommandData = new AutomationCommandValueData
            {
                AutomationCommand = SelectedAutomationCommand,
                Value = commandValue
            };

            automationCommandData.PublishEvent(EventTopicNames.HandlerRequested, true);
        }

        private void SetSelectedAutomationCommand(AutomationCommand command)
        {
            SelectedAutomationCommand = command;
            RaisePropertyChanged(() => SelectedAutomationCommand);
        }
    }
}
