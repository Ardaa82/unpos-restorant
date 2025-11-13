using Microsoft.Practices.Prism.Commands;
using Samba.Presentation.Common;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace Samba.Modules.FastPayModule
{
    [Export]
    public class FastPayModuleViewModel : ObservableObject
    {
        private string _activeUrl;
        public string ActiveUrl
        {
            get => _activeUrl;
            set { _activeUrl = value; RaisePropertyChanged(() => ActiveUrl); }
        }

        public ICommand FastPayCommand { get; private set; }

        [ImportingConstructor]
        public FastPayModuleViewModel()
        {
            FastPayCommand = new DelegateCommand(OnFastPayExecuted);
        }

        private void OnFastPayExecuted()
        {
            ActiveUrl = "https://www.fastpay.com.tr/"; // test amacıyla
        }
    }
}
