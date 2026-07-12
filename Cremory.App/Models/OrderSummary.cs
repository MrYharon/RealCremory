using System.ComponentModel;

namespace Cremory.App.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Creating = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class OrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Items { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string Source { get; set; } = "Walk-in";
        public string? CustomerContact { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class OrderSummary : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Notify(params string[] names)
        {
            foreach (var n in names)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }

        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Items { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public bool IsJustReceived { get; set; } = false;

        private OrderStatus _status = OrderStatus.Pending;
        public OrderStatus Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                Notify(nameof(Status), nameof(BorderColor), nameof(ActionButtonText),
                       nameof(ActionButtonColor), nameof(ShowActionButton),
                       nameof(StatusBadge), nameof(StatusColor), nameof(StatusMessage),
                       nameof(AvatarColor));
            }
        }

        public string Source { get; set; } = "Walk-in";
        public string? CustomerContact { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public static OrderSummary FromDto(OrderDto dto)
        {
            return new OrderSummary
            {
                OrderId = dto.OrderId,
                CustomerName = dto.CustomerName,
                Items = dto.Items,
                TotalPrice = dto.TotalPrice,
                Status = dto.Status,
                Source = dto.Source,
                CustomerContact = dto.CustomerContact,
                CreatedAtUtc = dto.CreatedAt,
                UpdatedAtUtc = dto.UpdatedAt,
                Timestamp = FormatRelativeTime(dto.CreatedAt),
                IsJustReceived = dto.CreatedAt > DateTime.UtcNow.AddMinutes(-2)
            };
        }

        private static string FormatRelativeTime(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;
            if (diff.TotalSeconds < 60) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} mins ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return dateTime.ToString("MMM dd");
        }

        public string StatusBadge => Status switch
        {
            OrderStatus.Pending => "PENDING",
            OrderStatus.Creating => "PREPARING",
            OrderStatus.Completed => "COMPLETE",
            OrderStatus.Cancelled => "CANCELLED",
            _ => "UNKNOWN"
        };

        public string StatusColor => Status switch
        {
            OrderStatus.Pending => "#FBC4C4",
            OrderStatus.Creating => "#E27575",
            OrderStatus.Completed => "#C4DFC4",
            OrderStatus.Cancelled => "#B89292",
            _ => "#B89292"
        };

        public string StatusMessage => Status switch
        {
            OrderStatus.Pending => "Waiting for kitchen to start preparation",
            OrderStatus.Creating => "Kitchen is working on this order",
            OrderStatus.Completed => "Order complete and ready",
            OrderStatus.Cancelled => "This order has been cancelled",
            _ => ""
        };

        public string ActionButtonText => Status switch
        {
            OrderStatus.Pending => "▶ Start Preparing",
            OrderStatus.Creating => "✓ Mark Complete",
            OrderStatus.Completed => "Completed",
            OrderStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };

        public string ActionButtonColor => Status switch
        {
            OrderStatus.Pending => "#8D6E63",
            OrderStatus.Creating => "#6B8F71",
            OrderStatus.Completed => "#CCCCCC",
            OrderStatus.Cancelled => "#CCCCCC",
            _ => "#999999"
        };

        public bool ShowActionButton => Status != OrderStatus.Completed && Status != OrderStatus.Cancelled;

        public static OrderStatus? NextStatus(OrderStatus current) => current switch
        {
            OrderStatus.Pending => OrderStatus.Creating,
            OrderStatus.Creating => OrderStatus.Completed,
            _ => null
        };

        public string AvatarColor => IsJustReceived ? "#E27575" : "#B89292";

        public string BorderColor => IsJustReceived ? "#E27575" : Status switch
        {
            OrderStatus.Pending => "#F0E0E0",
            OrderStatus.Creating => "#FBC4C4",
            OrderStatus.Completed => "#C4DFC4",
            OrderStatus.Cancelled => "#E0D0D0",
            _ => "#E0D0BC"
        };
    }
}
