using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_ASP.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace API_ASP.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminApiController : ControllerBase

    {
        private readonly ASPBDContext _context;

        public AdminApiController(ASPBDContext context)
        {
            _context = context;
        }

        #region Catalogs CRUD

        [HttpGet("catalogs")]
        public async Task<ActionResult<IEnumerable<Catalog>>> GetCatalogs()
        {
            return await _context.Catalogs.ToListAsync();
        }

        [HttpGet("catalogs/{id}")]
        public async Task<ActionResult<Catalog>> GetCatalog(int id)
        {
            var catalog = await _context.Catalogs.FindAsync(id);
            if (catalog == null) return NotFound();
            return catalog;
        }

        [HttpPost("catalogs")]
        public async Task<ActionResult<Catalog>> CreateCatalog([FromBody] Catalog catalog)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Catalogs.Add(catalog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCatalog), new { id = catalog.CatalogsId }, catalog);
        }

        [HttpPut("catalogs/{id}")]
        public async Task<IActionResult> UpdateCatalog(int id, [FromBody] Catalog catalog)
        {
            if (id != catalog.CatalogsId) return BadRequest();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Entry(catalog).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CatalogExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("catalogs/{id}")]
        public async Task<IActionResult> DeleteCatalog(int id)
        {
            var catalog = await _context.Catalogs.FindAsync(id);
            if (catalog == null) return NotFound();

            _context.Catalogs.Remove(catalog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CatalogExists(int id) => _context.Catalogs.Any(e => e.CatalogsId == id);

        #endregion

        #region Categories CRUD

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        [HttpGet("categories/{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return category;
        }

        [HttpPost("categories")]
        public async Task<ActionResult<Category>> CreateCategory([FromBody] Category category)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            if (id != category.CategoryId) return BadRequest();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id) => _context.Categories.Any(e => e.CategoryId == id);

        #endregion

        #region Orders CRUD

        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.Catalogs)
                .Include(o => o.Users)
                .ToListAsync();
        }

        [HttpGet("orders/{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Catalogs)
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();
            return order;
        }

        [HttpPost("orders")]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrdersId }, order);
        }

        [HttpPut("orders/{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
        {
            if (id != order.OrdersId) return BadRequest();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id) => _context.Orders.Any(e => e.OrdersId == id);

        #endregion

        #region PosOrders CRUD

        [HttpGet("posorders")]
        public async Task<ActionResult<IEnumerable<PosOrder>>> GetPosOrders()
        {
            return await _context.PosOrders
                .Include(p => p.Order)
                .Include(p => p.Product)
                .ToListAsync();
        }

        [HttpGet("posorders/{id}")]
        public async Task<ActionResult<PosOrder>> GetPosOrder(int id)
        {
            var posOrder = await _context.PosOrders
                .Include(p => p.Order)
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.PosOrderId == id);

            if (posOrder == null) return NotFound();
            return posOrder;
        }

        [HttpPost("posorders")]
        public async Task<ActionResult<PosOrder>> CreatePosOrder([FromBody] PosOrder posOrder)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.PosOrders.Add(posOrder);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPosOrder), new { id = posOrder.PosOrderId }, posOrder);
        }

        [HttpPut("posorders/{id}")]
        public async Task<IActionResult> UpdatePosOrder(int id, [FromBody] PosOrder posOrder)
        {
            if (id != posOrder.PosOrderId) return BadRequest();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Entry(posOrder).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PosOrderExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("posorders/{id}")]
        public async Task<IActionResult> DeletePosOrder(int id)
        {
            var posOrder = await _context.PosOrders.FindAsync(id);
            if (posOrder == null) return NotFound();

            _context.PosOrders.Remove(posOrder);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PosOrderExists(int id) => _context.PosOrders.Any(e => e.PosOrderId == id);

        #endregion

        #region Users CRUD

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();
            return user;
        }

        [HttpPost("users")]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            user.Password = HashPassword(user.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
        {
            if (id != user.UserId) return BadRequest();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            user.Password = HashPassword(user.Password);
            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id) => _context.Users.Any(e => e.UserId == id);

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }

        #endregion
    }
}