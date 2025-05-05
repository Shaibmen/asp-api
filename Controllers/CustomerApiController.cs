using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_ASP.Models;
using ASPNETKEK.Models.Dto;
using API_ASP.Models.Dto;
using ASPNETKEK.Models;

namespace ASPNETKEK.Controllers
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

        // GET: api/CustomerApi/catalog
        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog([FromQuery] string? category, [FromQuery] string? searchQuery, [FromQuery] string? sortBy)
        {
            var products = _context.Catalogs.Include(c => c.Categories).AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Categories.Any(c => c.CategoryName == category));
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                products = products.Where(p => p.Title.Contains(searchQuery) || p.Author.Contains(searchQuery) || p.Publisher.Contains(searchQuery));
            }

            products = sortBy switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products
            };

            return Ok(await products.ToListAsync());
        }

        // POST: api/CustomerApi/update-cart
        [HttpPost("update-cart")]
        public async Task<IActionResult> UpdateCartItem([FromBody] CartUpdateDto model)
        {
            var cartItem = await _context.PosOrders.FindAsync(model.PosOrderId);
            if (cartItem == null) return NotFound("Элемент корзины не найден.");

            if (model.NewCount <= 0)
            {
                _context.PosOrders.Remove(cartItem);
            }
            else
            {
                cartItem.Count = model.NewCount;
            }

            await _context.SaveChangesAsync();
            return Ok("Корзина обновлена");
        }

        // POST: api/CustomerApi/add-to-cart
        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto model)
        {
            var userLogin = User.Identity?.Name;
            if (userLogin == null) return Unauthorized("Неавторизован.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == userLogin);
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

            var cartItem = await _context.PosOrders.FirstOrDefaultAsync(c => c.ProductId == model.CatalogId && c.OrderId == order.OrdersId);
            if (cartItem == null)
            {
                cartItem = new PosOrder { ProductId = model.CatalogId, OrderId = order.OrdersId, Count = 1 };
                _context.PosOrders.Add(cartItem);
            }
            else
            {
                cartItem.Count += 1;
            }

            order.TotalSum += decimal.Parse(product.Price);
            await _context.SaveChangesAsync();

            return Ok("Добавлено в корзину");
        }

        // GET: api/CustomerApi/cart
        [HttpGet("cart")]
        public IActionResult GetCart()
        {
            var userLogin = User.Identity?.Name;
            if (userLogin == null) return Unauthorized("Неавторизован.");

            var cartItems = _context.PosOrders
                .Where(c => c.Order.Users.Login == userLogin)
                .Include(c => c.Product)
                .ToList();

            var totalSum = cartItems.Sum(c => c.Count * Convert.ToDecimal(c.Product.Price));

            return Ok(new { Items = cartItems, TotalSum = totalSum });
        }

        // GET: api/CustomerApi/product-details/5
        [HttpGet("product-details/{id}")]
        public IActionResult ProductDetails(int id)
        {
            var product = _context.Catalogs
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefault(p => p.CatalogsId == id);

            if (product == null) return NotFound();

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                Reviews = (List<Review>)product.Reviews
            };

            return Ok(viewModel);
        }

        // POST: api/CustomerApi/add-review
        [HttpPost("add-review")]
        public async Task<IActionResult> AddReview([FromBody] ReviewFormModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Некорректные данные.");

            var userLogin = User.Identity?.Name;
            if (userLogin == null) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == userLogin);
            if (user == null) return Unauthorized("Пользователь не найден.");

            var review = new Review
            {
                ProductId = model.ProductId,
                UserId = user.UserId,
                ReviewText = model.Text,
                Rating = model.Rating,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok("Отзыв добавлен");
        }
    }
}
