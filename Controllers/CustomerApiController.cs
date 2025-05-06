using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNETKEK.Models;
using API_ASP.Models;
using API_ASP.Models.Dto;
using ASPNETKEK.Models.Dto;

namespace API_ASP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerApiController : ControllerBase
    {
        private readonly ASPBDContext _context;

        public CustomerApiController(ASPBDContext context)
        {
            _context = context;
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog([FromQuery] string? category, [FromQuery] string? searchQuery, [FromQuery] string? sortBy)
        {
            var products = _context.Catalogs.Include(c => c.Categories).AsQueryable();

            if (!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Categories.Any(c => c.CategoryName == category));

            if (!string.IsNullOrEmpty(searchQuery))
                products = products.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Author.Contains(searchQuery) ||
                    p.Publisher.Contains(searchQuery));

            products = sortBy switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products
            };

            var result = await products.ToListAsync();
            return Ok(result);
        }

        [HttpPost("update-cart")]
        public async Task<IActionResult> UpdateCartItem([FromBody] CartUpdateDto model)
        {
            var cartItem = await _context.PosOrders.FindAsync(model.PosOrderId);
            if (cartItem == null)
                return NotFound("Элемент корзины не найден.");

            if (model.NewCount <= 0)
                _context.PosOrders.Remove(cartItem);
            else
                cartItem.Count = model.NewCount;

            await _context.SaveChangesAsync();
            return Ok("Корзина обновлена");
        }

        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(); // временно просто берём первого пользователя
            if (user == null) return NotFound("Пользователь не найден.");

            var product = await _context.Catalogs.FindAsync(model.CatalogId);
            if (product == null) return NotFound("Товар не найден.");

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.UsersId == user.UserId && !o.PosOrders.Any());

            if (order == null)
            {
                order = new Order { UsersId = user.UserId, TotalSum = 0 };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }

            var cartItem = await _context.PosOrders
                .FirstOrDefaultAsync(c => c.ProductId == model.CatalogId && c.OrderId == order.OrdersId);

            if (cartItem == null)
                _context.PosOrders.Add(new PosOrder { ProductId = model.CatalogId, OrderId = order.OrdersId, Count = 1 });
            else
                cartItem.Count += 1;

            order.TotalSum += decimal.Parse(product.Price);
            await _context.SaveChangesAsync();

            return Ok("Добавлено в корзину");
        }

        [HttpGet("cart")]
        public IActionResult GetCart()
        {
            var user = _context.Users.FirstOrDefault(); // временно
            if (user == null) return NotFound("Пользователь не найден.");

            var cartItems = _context.PosOrders
                .Where(c => c.Order.UsersId == user.UserId)
                .Include(c => c.Product)
                .ToList();

            var totalSum = cartItems.Sum(c => c.Count * Convert.ToDecimal(c.Product.Price));

            return Ok(new { Items = cartItems, TotalSum = totalSum });
        }

        [HttpGet("product-details/{id}")]
        public IActionResult ProductDetails(int id)
        {
            var product = _context.Catalogs
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefault(p => p.CatalogsId == id);

            if (product == null) return NotFound();

            return Ok(new ProductDetailsViewModel
            {
                Product = product,
                Reviews = product.Reviews.ToList()
            });
        }

        [HttpPost("add-review")]
        public async Task<IActionResult> AddReview([FromBody] ReviewFormModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Некорректные данные.");

            var user = await _context.Users.FirstOrDefaultAsync(); // временно
            if (user == null) return NotFound("Пользователь не найден.");

            _context.Reviews.Add(new Review
            {
                ProductId = model.ProductId,
                UserId = user.UserId,
                ReviewText = model.Text,
                Rating = model.Rating,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok("Отзыв добавлен");
        }
    }
}
