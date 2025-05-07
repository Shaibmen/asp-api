public class CartItemDto
{
    public int PosOrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductTitle { get; set; }
    public string ProductImage { get; set; }
    public decimal Price { get; set; }
    public int Count { get; set; }
}