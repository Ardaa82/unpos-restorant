using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain.Models.Entities;
using Samba.Presentation.Common;
using Samba.Presentation.Services;
using Samba.Services;

namespace Samba.Modules.PackageServiceModule
{
    // Basit ticket item VM (sadece bu ekran için)
    public class PackageTicketItemViewModel : ObservableObject
    {
        private bool _isSelected;
        private string _state;
        private string _courierName;

        public int TicketId { get; set; }
        public string CustomerName { get; set; }
        public decimal Total { get; set; }

        public string State
        {
            get { return _state; }
            set { _state = value; RaisePropertyChanged(() => State); }
        }

        public string CourierName
        {
            get { return _courierName; }
            set { _courierName = value; RaisePropertyChanged(() => CourierName); }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; RaisePropertyChanged(() => IsSelected); }
        }

        public string DisplayText
        {
            get
            {
                // Örn: #15 - Ali Yılmaz - 120,00
                return string.Format("#{0} - {1} - {2:N2}", TicketId, CustomerName, Total);
            }
        }
    }

    // Basit paketçi item VM (sadece bu ekran için)
    public class PackageCourierItemViewModel : ObservableObject
    {
        public int EntityId { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
    }

    [Export]
    public class PackageServiceViewModel : ObservableObject
    {
        private readonly IApplicationState _applicationState;
        private readonly ICacheService _cacheService;
        private readonly IEntityService _entityService;
        private readonly ITicketService _ticketService;

        private EntityType _customerEntityType;
        private EntityType _courierEntityType;

        private int _tempTicketCounter = 1;

        private string _customerSearchText;
        private string _courierSearchText;
        private Entity _selectedCustomer;
        private PackageCourierItemViewModel _selectedCourier;
        private PackageTicketItemViewModel _selectedInTransitTicket;
        private int _selectedTabIndex;

        public ObservableCollection<Entity> FoundCustomers { get; private set; }
        public ObservableCollection<PackageTicketItemViewModel> PendingTickets { get; private set; }
        public ObservableCollection<PackageCourierItemViewModel> Couriers { get; private set; }
        public ObservableCollection<PackageTicketItemViewModel> InTransitTickets { get; private set; }

        public DelegateCommand SearchCustomersCommand { get; private set; }
        public DelegateCommand AddTicketForSelectedCustomerCommand { get; private set; }
        public DelegateCommand SearchCouriersCommand { get; private set; }
        public DelegateCommand AssignTicketsToCourierCommand { get; private set; }
        public DelegateCommand OpenTicketCommand { get; private set; }
        public DelegateCommand MarkAsDeliveredCommand { get; private set; }

        [ImportingConstructor]
        public PackageServiceViewModel(
            IApplicationState applicationState,
            ICacheService cacheService,
            IEntityService entityService,
            ITicketService ticketService)
        {
            _applicationState = applicationState;
            _cacheService = cacheService;
            _entityService = entityService;
            _ticketService = ticketService;

            FoundCustomers = new ObservableCollection<Entity>();
            PendingTickets = new ObservableCollection<PackageTicketItemViewModel>();
            Couriers = new ObservableCollection<PackageCourierItemViewModel>();
            InTransitTickets = new ObservableCollection<PackageTicketItemViewModel>();

            InitializeEntityTypes();
            InitializeCommands();
            LoadInitialCouriers();
            // İstersen burada gerçek bekleyen / yoldaki adisyonları da çekebilirsin.
            // LoadPendingTicketsFromSystem();
        }

        public string HeaderText
        {
            get { return "Paket Servis"; }
        }

        public string Description
        {
            get
            {
                return "Telefonla gelen siparişleri müşteriler ve paketçiler arasında " +
                       "hızlıca dağıtmak için özel paket servis ekranı. " +
                       "İlk sekmede atama, ikinci sekmede yoldaki siparişler takip edilir.";
            }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                RaisePropertyChanged(() => SelectedTabIndex);
            }
        }

        public string CustomerSearchText
        {
            get { return _customerSearchText; }
            set
            {
                _customerSearchText = value;
                RaisePropertyChanged(() => CustomerSearchText);
                SearchCustomersCommand.RaiseCanExecuteChanged();
            }
        }

        public string CourierSearchText
        {
            get { return _courierSearchText; }
            set
            {
                _courierSearchText = value;
                RaisePropertyChanged(() => CourierSearchText);
                SearchCouriersCommand.RaiseCanExecuteChanged();
            }
        }

        public Entity SelectedCustomer
        {
            get { return _selectedCustomer; }
            set
            {
                _selectedCustomer = value;
                RaisePropertyChanged(() => SelectedCustomer);
                AddTicketForSelectedCustomerCommand.RaiseCanExecuteChanged();
            }
        }

        public PackageCourierItemViewModel SelectedCourier
        {
            get { return _selectedCourier; }
            set
            {
                _selectedCourier = value;
                RaisePropertyChanged(() => SelectedCourier);
                AssignTicketsToCourierCommand.RaiseCanExecuteChanged();
            }
        }

        public PackageTicketItemViewModel SelectedInTransitTicket
        {
            get { return _selectedInTransitTicket; }
            set
            {
                _selectedInTransitTicket = value;
                RaisePropertyChanged(() => SelectedInTransitTicket);
                OpenTicketCommand.RaiseCanExecuteChanged();
                MarkAsDeliveredCommand.RaiseCanExecuteChanged();
            }
        }

        private void InitializeEntityTypes()
        {
            var allTypes = _cacheService.GetEntityTypes().ToList();

            // Burada isimleri kendi sistemindeki EntityName / Name ile eşleştir.
            _customerEntityType = allTypes.FirstOrDefault(x =>
                string.Equals(x.EntityName, "Müşteri", StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(x.Name, "Müşteri", StringComparison.CurrentCultureIgnoreCase));

            _courierEntityType = allTypes.FirstOrDefault(x =>
                string.Equals(x.EntityName, "Paketçi", StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(x.Name, "Paketçi", StringComparison.CurrentCultureIgnoreCase));

            // Eğer farklı isimler kullanıyorsan burayı kendine göre değiştirmen gerekecek.
        }

        private void InitializeCommands()
        {
            SearchCustomersCommand = new DelegateCommand(OnSearchCustomers, CanSearchCustomers);
            AddTicketForSelectedCustomerCommand = new DelegateCommand(OnAddTicketForSelectedCustomer, CanAddTicketForSelectedCustomer);
            SearchCouriersCommand = new DelegateCommand(OnSearchCouriers, CanSearchCouriers);
            AssignTicketsToCourierCommand = new DelegateCommand(OnAssignTicketsToCourier, CanAssignTicketsToCourier);
            OpenTicketCommand = new DelegateCommand(OnOpenTicket, CanOpenTicket);
            MarkAsDeliveredCommand = new DelegateCommand(OnMarkAsDelivered, CanMarkAsDelivered);
        }

        private void LoadInitialCouriers()
        {
            Couriers.Clear();

            if (_courierEntityType == null)
                return;

            // Basit: tüm paketçileri çek. İstersen filtre, durum vs. ekleyebilirsin.
            var couriers = _entityService.SearchEntities(_courierEntityType, "", null);

            foreach (var e in couriers)
            {
                var region = e.CustomData != null
                    ? e.CustomData
                    : ""; // EntityCustomField'leri ihtiyacına göre işle.

                Couriers.Add(new PackageCourierItemViewModel
                {
                    EntityId = e.Id,
                    Name = e.Name,
                    Region = region
                });
            }
        }

        #region Müşteri Arama & Sipariş Ekle

        private bool CanSearchCustomers()
        {
            return _customerEntityType != null && !string.IsNullOrWhiteSpace(CustomerSearchText);
        }

        private void OnSearchCustomers()
        {
            FoundCustomers.Clear();
            if (_customerEntityType == null) return;

            var customers = _entityService.SearchEntities(_customerEntityType, CustomerSearchText, null);
            foreach (var c in customers)
                FoundCustomers.Add(c);
        }

        private bool CanAddTicketForSelectedCustomer()
        {
            return SelectedCustomer != null;
        }

        // Şu an sadece ekranda sahte bir "Bekliyor" ticket oluşturuyor.
        // Gerçek kullanımda burada ITicketService ile ticket oluşturup state'leri ayarlayacaksın.
        private void OnAddTicketForSelectedCustomer()
        {
            if (SelectedCustomer == null) return;

            var item = new PackageTicketItemViewModel
            {
                TicketId = _tempTicketCounter++, // geçici ID
                CustomerName = SelectedCustomer.Name,
                Total = 0m,
                State = "Bekliyor",
                CourierName = ""
            };

            PendingTickets.Add(item);
        }

        #endregion

        #region Paketçi Arama & Atama

        private bool CanSearchCouriers()
        {
            return _courierEntityType != null; // İstersen burada text şartı da koy.
        }

        private void OnSearchCouriers()
        {
            Couriers.Clear();
            if (_courierEntityType == null) return;

            var text = CourierSearchText ?? "";
            var couriers = _entityService.SearchEntities(_courierEntityType, text, null);

            foreach (var e in couriers)
            {
                var region = e.CustomData != null
                    ? e.CustomData
                    : "";

                Couriers.Add(new PackageCourierItemViewModel
                {
                    EntityId = e.Id,
                    Name = e.Name,
                    Region = region
                });
            }
        }

        private bool CanAssignTicketsToCourier()
        {
            return SelectedCourier != null &&
                   PendingTickets.Any(x => x.IsSelected);
        }

        private void OnAssignTicketsToCourier()
        {
            if (SelectedCourier == null) return;

            var selectedTickets = PendingTickets.Where(x => x.IsSelected).ToList();
            if (!selectedTickets.Any()) return;

            foreach (var t in selectedTickets)
            {
                t.CourierName = SelectedCourier.Name;
                t.State = "Yolda";
                t.IsSelected = false;

                PendingTickets.Remove(t);
                InTransitTickets.Add(t);

                // Gerçek sistemde:
                // - ticket'i yükle (ITicketService.LoadTicket / OpenTicket vs.)
                // - entity "Paketçi" varlığını değiştir
                // - state "Yolda" yap
                // - ticket'i kaydet ve kapat
            }

            // Atama sonrası otomatik olarak 2. sekmeye geç
            SelectedTabIndex = 1;
        }

        #endregion

        #region Yoldaki Siparişler: Tahsilat & Teslim

        private bool CanOpenTicket()
        {
            return SelectedInTransitTicket != null;
        }

        private void OnOpenTicket()
        {
            if (SelectedInTransitTicket == null) return;

            // Gerçek kullanımda burada ilgili TicketId ile POS ekranını açman gerekiyor.
            // Ör: _ticketService.OpenTicket(SelectedInTransitTicket.TicketId); + event publish.
        }

        private bool CanMarkAsDelivered()
        {
            return SelectedInTransitTicket != null;
        }

        private void OnMarkAsDelivered()
        {
            if (SelectedInTransitTicket == null) return;

            // Sadece ekrandaki state'i güncelliyor;
            // Gerçek sistemde ticket state + ödeme + kapatma yapman gerekir.
            SelectedInTransitTicket.State = "Teslim";

            // İstersen listeden de düşebilirsin:
            // InTransitTickets.Remove(SelectedInTransitTicket);
        }

        #endregion
    }
}
