using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_ASP.Models;
using System.Security.Claims;

namespace API_ASP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ASPBDContext _context;

        public CustomerController(ASPBDContext context)
        {
            _context = context;
        }

        // GET: api/customer/catalog
        [HttpGet("catalog")]
        public async Task<ActionResult<IEnumerable<Catalog>>> GetCatalog(
            [FromQuery] string? category,
            [FromQuery] string? searchQuery,
            [FromQuery] string? sortBy)
        {
            var products = _context.Catalogs.Include(c => c.Categories).AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Categories.Any(c => c.CategoryName == category));
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                products = products.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Author.Contains(searchQuery) ||
                    p.Publisher.Contains(searchQuery));
            }

            switch (sortBy)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
            }

            return await products.ToListAsync();
        }

        // POST: api/customer/update-cart
        [HttpPost("update-cart")]
        public async Task<IActionResult> UpdateCartItem([FromBody] CartUpdateRequest request)
        {
            var cartItem = await _context.PosOrders.FindAsync(request.PosOrderId);
            if (cartItem == null)
            {
                return NotFound(new { Message = "Элемент корзины не найден." });
            }

            if (request.NewCount <= 0)
            {
                _context.PosOrders.Remove(cartItem);
            }
            else
            {
                cartItem.Count = request.NewCount;
            }

            await _context.SaveChangesAsync();

            // Обновляем общую сумму заказа
            await UpdateOrderTotal((int)cartItem.OrderId);

            return Ok(new { Message = "Корзина успешно обновлена" });
        }

        // POST: api/customer/add-to-cart
        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(new { Message = "Пользователь не авторизован" });
            }

            if (!int.TryParse(userId, out var intUserId))
            {
                return BadRequest(new { Message = "Некорректный идентификатор пользователя" });
            }

            var product = await _context.Catalogs.FindAsync(request.CatalogId);
            if (product == null)
            {
                return NotFound(new { Message = "Товар не найден." });
            }

            // Ищем активный заказ пользователя (без проверки IsCompleted, так как его нет в модели)
            var order = await _context.Orders
                .Include(o => o.PosOrders)
                .FirstOrDefaultAsync(o => o.UsersId == intUserId && o.PosOrders.Any());

            if (order == null)
            {
                order = new Order
                {
                    UsersId = intUserId,
                    TotalSum = 0
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }

            var cartItem = await _context.PosOrders
                .FirstOrDefaultAsync(c => c.ProductId == request.CatalogId && c.OrderId == order.OrdersId);

            if (cartItem == null)
            {
                cartItem = new PosOrder
                {
                    ProductId = request.CatalogId,
                    OrderId = order.OrdersId,
                    Count = 1
                };
                _context.PosOrders.Add(cartItem);
            }
            else
            {
                cartItem.Count += 1;
            }

            await _context.SaveChangesAsync();
            await UpdateOrderTotal(order.OrdersId);

            return Ok(new { Message = "Товар успешно добавлен в корзину" });
        }

        // GET: api/customer/cart
        [HttpGet("cart")]
        public async Task<ActionResult<CartResponse>> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(new { Message = "Пользователь не авторизован" });
            }

            if (!int.TryParse(userId, out var intUserId))
            {
                return BadRequest(new { Message = "Некорректный идентификатор пользователя" });
            }

            // Находим заказ пользователя с позициями
            var order = await _context.Orders
                .Include(o => o.PosOrders)
                .ThenInclude(po => po.Product)
                .FirstOrDefaultAsync(o => o.UsersId == intUserId && o.PosOrders.Any());

            if (order == null)
            {
                return Ok(new CartResponse { Items = new List<PosOrder>(), TotalSum = 0 });
            }

            return new CartResponse
            {
                Items = order.PosOrders.ToList(),
                TotalSum = order.TotalSum
            };
        }

        // GET: api/customer/product-details/{id}
        [HttpGet("product-details/{id}")]
        public async Task<ActionResult<ProductDetailsResponse>> GetProductDetails(int id)
        {
            var product = await _context.Catalogs
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.CatalogsId == id);

            if (product == null)
            {
                return NotFound();
            }

            return new ProductDetailsResponse
            {
                Product = product,
                Reviews = product.Reviews.ToList()
            };
        }

        // POST: api/customer/add-review
        [HttpPost("add-review")]
        public async Task<IActionResult> AddReview([FromBody] ReviewRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Некорректные данные." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var intUserId))
            {
                return Unauthorized(new { Message = "Пользователь не авторизован" });
            }

            var product = await _context.Catalogs.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound(new { Message = "Товар не найден." });
            }

            var review = new Review
            {
                ProductId = request.ProductId,
                UserId = intUserId,
                ReviewText = request.Text,
                Rating = request.Rating,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Отзыв успешно добавлен" });
        }

        // GET: api/customer/average-rating/{productId}
        [HttpGet("average-rating/{productId}")]
        public async Task<ActionResult<AverageRatingResponse>> GetAverageRating(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            return new AverageRatingResponse { AverageRating = averageRating };
        }

        private async Task UpdateOrderTotal(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.PosOrders)
                .ThenInclude(po => po.Product)
                .FirstOrDefaultAsync(o => o.OrdersId == orderId);

            if (order != null)
            {
                order.TotalSum = order.PosOrders.Sum(po =>
                    po.Count * decimal.Parse(po.Product.Price));
                await _context.SaveChangesAsync();
            }
        }
    }

    // DTO классы
    public class CartUpdateRequest
    {
        public int PosOrderId { get; set; }
        public int NewCount { get; set; }
    }

    public class AddToCartRequest
    {
        public int CatalogId { get; set; }
    }

    public class CartResponse
    {
        public IEnumerable<PosOrder> Items { get; set; }
        public decimal TotalSum { get; set; }
    }

    public class ProductDetailsResponse
    {
        public Catalog Product { get; set; }
        public IEnumerable<Review> Reviews { get; set; }
    }

    public class ReviewRequest
    {
        public int ProductId { get; set; }
        public string Text { get; set; }
        public int Rating { get; set; }
    }

    public class AverageRatingResponse
    {
        public double AverageRating { get; set; }
    }
}