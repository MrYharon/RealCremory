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

    public class OrderSummary
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Items { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public bool IsJustReceived { get; set; } = false;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
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

        public string StatusBadge
        {
            get => Status switch
            {
                OrderStatus.Pending => "PENDING",
                OrderStatus.Creating => "PREPARING",
                OrderStatus.Completed => "COMPLETE",
                OrderStatus.Cancelled => "CANCELLED",
                _ => "UNKNOWN"
            };
        }

        public string StatusColor
        {
            get => Status switch
            {
                OrderStatus.Pending => "#FBC4C4",
                OrderStatus.Creating => "#E27575",
                OrderStatus.Completed => "#C4FBD5",
                OrderStatus.Cancelled => "#B89292",
                _ => "#B89292"
            };
        }

        public string StatusMessage
        {
            get => Status switch
            {
                OrderStatus.Pending => "Waiting for kitchen to start preparation",
                OrderStatus.Creating => "Kitchen is working on this order",
                OrderStatus.Completed => "Order complete and ready",
                OrderStatus.Cancelled => "This order has been cancelled",
                _ => ""
            };
        }

        public string ActionButtonText
        {
            get => Status switch
            {
                OrderStatus.Pending => "▶ Start Preparing",
                OrderStatus.Creating => "✓ Mark Complete",
                OrderStatus.Completed => "Completed",
                OrderStatus.Cancelled => "Cancelled",
                _ => "Unknown"
            };
        }

        public string ActionButtonColor
        {
            get => Status switch
            {
                OrderStatus.Pending => "#8D6E63",
                OrderStatus.Creating => "#2E7D32",
                OrderStatus.Completed => "#CCCCCC",
                OrderStatus.Cancelled => "#CCCCCC",
                _ => "#999999"
            };
        }

        public bool ShowActionButton
        {
            get => Status != OrderStatus.Completed && Status != OrderStatus.Cancelled;
        }

        public static OrderStatus? NextStatus(OrderStatus current) => current switch
        {
            OrderStatus.Pending => OrderStatus.Creating,
            OrderStatus.Creating => OrderStatus.Completed,
            _ => null
        };

        public string AvatarColor
        {
            get => IsJustReceived ? "#E27575" : "#B89292";
        }

        public string BorderColor
        {
            get => IsJustReceived ? "#E27575" : Status switch
            {
                OrderStatus.Pending => "#F0E0E0",
                OrderStatus.Creating => "#FBC4C4",
                OrderStatus.Completed => "#C4FBD5",
                OrderStatus.Cancelled => "#E0D0D0",
                _ => "#E0D0BC"
            };
        }
    }
}
