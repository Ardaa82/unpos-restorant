using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain.Models.Entities;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Modules.PackageServiceModule
{
    /// <summary>
    /// Paket Servis özel ekran VM’i.
    /// TAB 1: Müşteri arama + "Seçili müşteriye sipariş ekle"
    /// TAB 2: Paketçi listesi + Bekleyen/Yoldaki sipariş listeleri.
    /// 
    /// Ticket oluşturma / adisyon açma / state değiştirme işleri
    /// özellikle TODO bırakıldı; bunları tutorial’daki Automation Action + Rule
    /// tarafına bağlaman gerekecek.
    /// </summary>
    [Export]
    public class PackageServiceViewModel : ObservableObject
    {
        private readonly IEntityService _entityService;
        private readonly ICacheService _cacheService;

        private readonly EntityType _customerEntityType;
        private readonly EntityType _courierEntityType;

        private string _customerSearchText;
        private string _courierSearchText;

        private Entity _selectedCustomer;
        private Entity _selectedCourier;

        private PackageOrderInfo _selectedPendingOrder;
        private PackageOrderInfo _selectedOnTheWayOrder;

        // DelegateCommand referansları (ObservesProperty yerine RaiseCanExecuteChanged kullanacağız)
        private readonly DelegateCommand _searchCustomersCommand;
        private readonly DelegateCommand _searchCouriersCommand;
        private readonly DelegateCommand _addOrderForSelectedCustomerCommand;
        private readonly DelegateCommand _assignCourierToSelectedOrdersCommand;
        private readonly DelegateCommand _goToTicketCommand;
        private readonly DelegateCommand _markDeliveredCommand;

        [ImportingConstructor]
        public PackageServiceViewModel(IEntityService entityService, ICacheService cacheService)
        {
            _entityService = entityService;
            _cacheService = cacheService;

            CustomerResults = new ObservableCollection<Entity>();
            CourierResults = new ObservableCollection<Entity>();
            PendingOrders = new ObservableCollection<PackageOrderInfo>();
            OnTheWayOrders = new ObservableCollection<PackageOrderInfo>();

            // Komutların oluşturulması
            _searchCustomersCommand = new DelegateCommand(RefreshCustomers, CanSearchCustomers);
            _searchCouriersCommand = new DelegateCommand(RefreshCouriers, CanSearchCouriers);
            _addOrderForSelectedCustomerCommand = new DelegateCommand(OnAddOrderForSelectedCustomer, CanAddOrderForSelectedCustomer);
            _assignCourierToSelectedOrdersCommand = new DelegateCommand(OnAssignCourierToSelectedOrders, CanAssignCourierToSelectedOrders);
            _goToTicketCommand = new DelegateCommand(OnGoToTicket, CanGoToTicket);
            _markDeliveredCommand = new DelegateCommand(OnMarkDelivered, CanMarkDelivered);

            // Tutorial’deki isimler:
            //  - "Müşteri" EntityType => müşteri
            //  - "Paketçi" EntityType => kurye
            _customerEntityType = FindEntityTypeByName("Müşteri");
            _courierEntityType = FindEntityTypeByName("Paketçi");
        }

        private EntityType FindEntityTypeByName(string name)
        {
            // Bulamazsa null döner; arama fonksiyonları null’da hiç çalışmayacak
            return _cacheService.GetEntityTypes().FirstOrDefault(x => x.Name == name);
        }

        #region Koleksiyonlar / seçimler

        public ObservableCollection<Entity> CustomerResults { get; private set; }
        public ObservableCollection<Entity> CourierResults { get; private set; }

        public ObservableCollection<PackageOrderInfo> PendingOrders { get; private set; }
        public ObservableCollection<PackageOrderInfo> OnTheWayOrders { get; private set; }

        public Entity SelectedCustomer
        {
            get { return _selectedCustomer; }
            set
            {
                _selectedCustomer = value;
                RaisePropertyChanged(() => SelectedCustomer);
                _addOrderForSelectedCustomerCommand.RaiseCanExecuteChanged();
            }
        }

        public Entity SelectedCourier
        {
            get { return _selectedCourier; }
            set
            {
                _selectedCourier = value;
                RaisePropertyChanged(() => SelectedCourier);
                _assignCourierToSelectedOrdersCommand.RaiseCanExecuteChanged();
            }
        }

        public PackageOrderInfo SelectedPendingOrder
        {
            get { return _selectedPendingOrder; }
            set
            {
                _selectedPendingOrder = value;
                RaisePropertyChanged(() => SelectedPendingOrder);
                _assignCourierToSelectedOrdersCommand.RaiseCanExecuteChanged();
            }
        }

        public PackageOrderInfo SelectedOnTheWayOrder
        {
            get { return _selectedOnTheWayOrder; }
            set
            {
                _selectedOnTheWayOrder = value;
                RaisePropertyChanged(() => SelectedOnTheWayOrder);
                _goToTicketCommand.RaiseCanExecuteChanged();
                _markDeliveredCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Arama metinleri

        public string CustomerSearchText
        {
            get { return _customerSearchText; }
            set
            {
                _customerSearchText = value;
                RaisePropertyChanged(() => CustomerSearchText);
                _searchCustomersCommand.RaiseCanExecuteChanged();
            }
        }

        public string CourierSearchText
        {
            get { return _courierSearchText; }
            set
            {
                _courierSearchText = value;
                RaisePropertyChanged(() => CourierSearchText);
                _searchCouriersCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Komutlar (ICommand olarak expose)

        public ICommand SearchCustomersCommand { get { return _searchCustomersCommand; } }
        public ICommand SearchCouriersCommand { get { return _searchCouriersCommand; } }

        public ICommand AddOrderForSelectedCustomerCommand { get { return _addOrderForSelectedCustomerCommand; } }
        public ICommand AssignCourierToSelectedOrdersCommand { get { return _assignCourierToSelectedOrdersCommand; } }

        public ICommand GoToTicketCommand { get { return _goToTicketCommand; } }
        public ICommand MarkDeliveredCommand { get { return _markDeliveredCommand; } }

        #endregion

        #region Komut implementasyonları

        private bool CanSearchCustomers()
        {
            return !string.IsNullOrWhiteSpace(CustomerSearchText) && _customerEntityType != null;
        }

        private void RefreshCustomers()
        {
            CustomerResults.Clear();

            if (!CanSearchCustomers())
                return;

            var term = CustomerSearchText.Trim();

            // EntitySearchViewModel ile aynı backend, ama stateFilter NULL DEĞİL.
            var entities = _entityService.SearchEntities(_customerEntityType, term, string.Empty);

            foreach (var e in entities)
            {
                CustomerResults.Add(e);
            }
        }

        private bool CanSearchCouriers()
        {
            return !string.IsNullOrWhiteSpace(CourierSearchText) && _courierEntityType != null;
        }

        private void RefreshCouriers()
        {
            CourierResults.Clear();

            if (!CanSearchCouriers())
                return;

            var term = CourierSearchText.Trim();
            var entities = _entityService.SearchEntities(_courierEntityType, term, string.Empty);

            foreach (var e in entities)
            {
                CourierResults.Add(e);
            }
        }

        private bool CanAddOrderForSelectedCustomer()
        {
            return SelectedCustomer != null;
        }

        private void OnAddOrderForSelectedCustomer()
        {
            if (SelectedCustomer == null)
                return;

            // BURASI BİLEREK BOŞ: 
            //  - Yeni ticket açma
            //  - Müşteri entity’sini ticket’a bağlama
            //  - TicketView’e gitme
            //  - Ticket kapatılınca bu ekrana dönme
            //
            // Bunları tutorial’deki gibi Automation Command + Action + Rule ile çözmen gerekiyor:
            //  * "Belge Oluşturuldu" / CreateTicket
            //  * "Belge Durumunu Değiştir" => PStatus = "Bekliyor-1"
            //  * "Belge Varlığını Değiştir" => Müşteri entity’sini ekle
        }

        private bool CanAssignCourierToSelectedOrders()
        {
            return SelectedCourier != null && SelectedPendingOrder != null;
        }

        private void OnAssignCourierToSelectedOrders()
        {
            if (SelectedCourier == null || SelectedPendingOrder == null)
                return;

            // Asıl doğrusu:
            //  - Her seçili pending ticket için "Belge Varlığını Değiştir" ile Paketçi ekle
            //  - PStatus = "Yolda" yap
            //
            // Şimdilik sadece VM içi modelde güncelliyorum, UI akışı otursun diye.

            SelectedPendingOrder.CourierName = SelectedCourier.Name;
            SelectedPendingOrder.State = "Yolda";

            OnTheWayOrders.Add(SelectedPendingOrder);
            PendingOrders.Remove(SelectedPendingOrder);

            SelectedPendingOrder = null;
        }

        private bool CanGoToTicket()
        {
            return SelectedOnTheWayOrder != null;
        }

        private void OnGoToTicket()
        {
            if (SelectedOnTheWayOrder == null)
                return;

            // Asıl akış:
            //  - Tutorial’deki "Adisyonu Görüntüle" action + rule ile aynı işi yap
            //  - TicketId => [:Value] üzerinden gönderiliyor
            //
            // Buradan ilgili Automation Command’ı tetikleyip
            // TicketView’e geçişi aynı hat üzerinden kurmalısın.
        }

        private bool CanMarkDelivered()
        {
            return SelectedOnTheWayOrder != null;
        }

        private void OnMarkDelivered()
        {
            if (SelectedOnTheWayOrder == null)
                return;

            // Asıl akış:
            //  - Belge Durumunu Değiştir => "Teslim"
            //  - Gerekirse tahsilat / ödeme / adisyon kapatma
            SelectedOnTheWayOrder.State = "Teslim";

            // İstersen listeden tamamen kaldırmayıp state’e göre filtreleyebilirsin.
            OnTheWayOrders.Remove(SelectedOnTheWayOrder);
            SelectedOnTheWayOrder = null;
        }

        #endregion
    }

    /// <summary>
    /// Ekranda gösterilen paket servis satırı: TicketId + müşteri + paketçi + toplam + durum.
    /// Ticket domain modelinden ayrı, sadece UI için hafif bir model.
    /// </summary>
    public class PackageOrderInfo : ObservableObject
    {
        private int _ticketId;
        private string _customerName;
        private string _courierName;
        private decimal _total;
        private string _state;

        public int TicketId
        {
            get { return _ticketId; }
            set
            {
                _ticketId = value;
                RaisePropertyChanged(() => TicketId);
            }
        }

        public string CustomerName
        {
            get { return _customerName; }
            set
            {
                _customerName = value;
                RaisePropertyChanged(() => CustomerName);
            }
        }

        public string CourierName
        {
            get { return _courierName; }
            set
            {
                _courierName = value;
                RaisePropertyChanged(() => CourierName);
            }
        }

        public decimal Total
        {
            get { return _total; }
            set
            {
                _total = value;
                RaisePropertyChanged(() => Total);
            }
        }

        public string State
        {
            get { return _state; }
            set
            {
                _state = value;
                RaisePropertyChanged(() => State);
            }
        }
    }
}
