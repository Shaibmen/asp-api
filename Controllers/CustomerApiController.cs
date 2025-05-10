using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_ASP.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace API_ASP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Требуем авторизацию для всех методов
    public class CustomerController : ControllerBase
    {
        private readonly ASPBDContext _context;

        public CustomerController(ASPBDContext context)
        {
            _context = context;
        }

        // GET: api/customer/catalog
        [HttpGet("catalog")]
        [AllowAnonymous] // Разрешаем доступ без авторизации
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
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Message = "Пользователь не авторизован" });

            var cartItem = await _context.PosOrders
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PosOrderId == request.PosOrderId && p.Order.UsersId == userId);

            if (cartItem == null)
                return NotFound(new { Message = "Элемент корзины не найден." });

            if (request.NewCount <= 0)
            {
                _context.PosOrders.Remove(cartItem);
            }
            else
            {
                cartItem.Count = request.NewCount;
            }

            await _context.SaveChangesAsync();
            await UpdateOrderTotal((int)cartItem.OrderId);

            return Ok(new { Message = "Корзина успешно обновлена" });
        }

        // POST: api/customer/add-to-cart
        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Message = "Пользователь не авторизован" });

            var product = await _context.Catalogs.FindAsync(request.CatalogId);
            if (product == null)
                return NotFound(new { Message = "Товар не найден" });

            var order = await _context.Orders
                .Include(o => o.PosOrders)
                .FirstOrDefaultAsync(o => o.UsersId == userId) ?? new Order
                {
                    UsersId = userId.Value,
                    TotalSum = 0
                };

            if (order.OrdersId == 0)
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }

            var cartItem = order.PosOrders.FirstOrDefault(p => p.ProductId == request.CatalogId);

            if (cartItem == null)
            {
                cartItem = new PosOrder
                {
                    OrderId = order.OrdersId,
                    ProductId = request.CatalogId,
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

            return Ok(new
            {
                Message = "Товар добавлен в корзину",
                CartItemId = cartItem.PosOrderId
            });
        }

        // GET: api/customer/cart
        [HttpGet("cart")]
        public async Task<ActionResult<CartResponse>> GetCart()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Message = "Пользователь не авторизован" });

            var order = await _context.Orders
                .Include(o => o.PosOrders)
                .ThenInclude(po => po.Product)
                .FirstOrDefaultAsync(o => o.UsersId == userId && o.PosOrders.Any());

            if (order == null)
                return Ok(new CartResponse { Items = new List<PosOrder>(), TotalSum = 0 });

            return Ok(new CartResponse
            {
                Items = order.PosOrders.ToList(),
                TotalSum = order.TotalSum
            });
        }

        // GET: api/customer/product-details/{id}
        [HttpGet("product-details/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductDetailsResponse>> GetProductDetails(int id)
        {
            var product = await _context.Catalogs
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.CatalogsId == id);

            if (product == null)
                return NotFound();

            return Ok(new ProductDetailsResponse
            {
                Product = product,
                Reviews = product.Reviews.ToList()
            });
        }

        // POST: api/customer/add-review
        [HttpPost("add-review")]
        public async Task<IActionResult> AddReview([FromBody] ReviewRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Некорректные данные." });

            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Message = "Пользователь не авторизован" });

            var product = await _context.Catalogs.FindAsync(request.ProductId);
            if (product == null)
                return NotFound(new { Message = "Товар не найден." });

            var review = new Review
            {
                ProductId = request.ProductId,
                UserId = userId.Value,
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
        [AllowAnonymous]
        public async Task<ActionResult<AverageRatingResponse>> GetAverageRating(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            return Ok(new AverageRatingResponse { AverageRating = averageRating });
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
                    po.Count * decimal.Parse(po.Product?.Price ?? "0"));
                await _context.SaveChangesAsync();
            }
        }

        private int? GetCurrentUserId()
        {
            // Пробуем разные варианты получения ID пользователя
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                            User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
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
        public IEnumerable<PosOrder> Items { get; set; } = new List<PosOrder>();
        public decimal TotalSum { get; set; }
    }

    public class ProductDetailsResponse
    {
        public Catalog Product { get; set; }
        public IEnumerable<Review> Reviews { get; set; } = new List<Review>();
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