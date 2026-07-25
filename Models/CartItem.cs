namespace E_Commerce_API.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int Quantity { get; set; }
        public int OriginalQuantity { get; set; } // ← الكمية الأصلية المطلوبة
        public int UserId { get; set; }
        public User User { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
