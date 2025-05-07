public class CartResponseDto
{
    public List<CartItemDto> Items { get; set; }
    public decimal TotalSum { get; set; }
}