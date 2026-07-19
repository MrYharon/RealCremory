using System.ComponentModel;

namespace Cremory.App.Models
{
    public class CartItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Notify(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Variant { get; set; }
        public string? Flavor { get; set; }
        public decimal BasePrice { get; set; }

        private int _qty = 1;
        public int Qty
        {
            get => _qty;
            set
            {
                if (_qty == value) return;
                _qty = value;
                Notify(nameof(Qty));
                Notify(nameof(Subtotal));
                Notify(nameof(DisplayText));
            }
        }

        public decimal Subtotal => BasePrice * Qty;

        public string DisplayText
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Flavor)) parts.Add(Flavor);
                parts.Add(Name);
                if (!string.IsNullOrWhiteSpace(Variant)) parts.Add(Variant);
                return $"{Qty}x {string.Join(" · ", parts)}";
            }
        }

        public string PriceText => $"₱{Subtotal:N2}";
        public string UnitPriceText => $"₱{BasePrice:N2} each";
    }
}
