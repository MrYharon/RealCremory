namespace Cremory.App.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Creating = 1,
        Completed = 2
    }

    public class OrderSummary
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Items { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Timestamp { get; set; } = string.Empty;

        // Flag to highlight brand new incoming orders
        public bool IsJustReceived { get; set; } = false;

        // Order state for the workflow state machine
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // UI Presentation Properties
        public string StatusBadge
        {
            get => Status switch
            {
                OrderStatus.Pending => "PENDING",
                OrderStatus.Creating => "PREPARING",
                OrderStatus.Completed => "COMPLETE",
                _ => "UNKNOWN"
            };
        }

        public string StatusColor
        {
            get => Status switch
            {
                OrderStatus.Pending => "#FF9800",    // Orange
                OrderStatus.Creating => "#2196F3",   // Blue
                OrderStatus.Completed => "#4CAF50",  // Green
                _ => "#999999"
            };
        }

        public string StatusMessage
        {
            get => Status switch
            {
                OrderStatus.Pending => "⏳ Waiting for kitchen staff to start preparation",
                OrderStatus.Creating => "👨‍🍳 Kitchen is actively working on this order",
                OrderStatus.Completed => "✅ Order complete and ready!",
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
                _ => "Unknown"
            };
        }

        public string ActionButtonColor
        {
            get => Status switch
            {
                OrderStatus.Pending => "#2196F3",    // Blue
                OrderStatus.Creating => "#4CAF50",   // Green
                OrderStatus.Completed => "#CCCCCC",  // Gray (disabled-look)
                _ => "#999999"
            };
        }

        public string AvatarColor
        {
            get => IsJustReceived ? "#FF5722" : "#1877F2";  // Red for new orders, Facebook blue otherwise
        }

        public string BorderColor
        {
            get => IsJustReceived ? "#FF5722" : Status switch
            {
                OrderStatus.Pending => "#FF9800",    // Orange
                OrderStatus.Creating => "#2196F3",   // Blue
                OrderStatus.Completed => "#4CAF50",  // Green
                _ => "#E0E0E0"
            };
        }
    }
}