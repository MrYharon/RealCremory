using System.Collections.ObjectModel;
using Cremory.App.Models;

namespace Cremory.App
{
    public partial class MainPage : ContentPage
    {
        // ObservableCollection automatically refreshes the UI when items are added or removed
        public ObservableCollection<OrderSummary> ActiveOrders { get; set; }

        // Analytics properties
        public decimal TotalProfitToday { get; set; } = 1500.00m;
        public int TotalOrdersToday { get; set; } = 8;

        public MainPage()
        {
            InitializeComponent();

            ActiveOrders = new ObservableCollection<OrderSummary>();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 1. Load any orders that were pending before the app opened
            LoadInitialOrders();

            // 2. Start listening for incoming Facebook commands
            SimulateIncomingFacebookOrder();
        }

        private void LoadInitialOrders()
        {
            // Clear to prevent duplicates on navigation
            ActiveOrders.Clear();

            ActiveOrders.Add(new OrderSummary
            {
                OrderId = "ORD-FB-001",
                CustomerName = "Walk-in Customer",
                Items = "2 x Classic Pandesal (Pack of 10)",
                TotalPrice = 100.00m,
                Timestamp = "10 mins ago",
                Status = OrderStatus.Creating
            });
        }

        private async void SimulateIncomingFacebookOrder()
        {
            // Wait 5 seconds to simulate your friend finishing the chat and sending "!order summary:"
            await Task.Delay(5000);

            // Insert the new order at the TOP of the list (index 0)
            ActiveOrders.Insert(0, new OrderSummary
            {
                OrderId = "ORD-FB-002",
                CustomerName = "Facebook Messenger Order",
                Items = "1 x blueberry cheesecake\n2 x Korean bun cheesecake",
                TotalPrice = 450.00m,
                Timestamp = "Just now",
                IsJustReceived = true,
                Status = OrderStatus.Pending
            });
        }

        private async void OnOrderActionClicked(object sender, EventArgs e)
        {
            // Grab the specific button that was clicked
            var button = sender as Button;

            // Extract the Order data tied to that specific button
            var order = button?.BindingContext as OrderSummary;

            if (order != null)
            {
                // State machine: move to next state
                switch (order.Status)
                {
                    case OrderStatus.Pending:
                        // User clicked "Start Preparing"
                        order.Status = OrderStatus.Creating;
                        break;

                    case OrderStatus.Creating:
                        // User clicked "Mark Complete"
                        order.Status = OrderStatus.Completed;
                        // Remove from active queue and send to archives
                        ActiveOrders.Remove(order);
                        await DisplayAlert("Order Complete", $"{order.OrderId} moved to archives.", "OK");
                        // TODO: Send to OrderArchivesPage and deduct ingredients from Oracle DB
                        break;

                    case OrderStatus.Completed:
                        // Already completed, shouldn't reach here
                        break;
                }

                // Refresh the UI by triggering property change
                OnPropertyChanged(nameof(ActiveOrders));
            }
        }

        
    }
}