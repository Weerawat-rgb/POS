using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Web.Data;
using POS.Web.Models.Entities;

namespace POS.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly POSDbContext _context;

        public ProductController(POSDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Category != null ? p.Category.Name : "")  // จัดการกรณี Category เป็น null
                .ThenBy(p => p.Name)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return Json(new
            {
                id = product.Id,
                name = product.Name,
                barcode = product.Barcode,
                price = product.Price,
                stock = product.Stock,
                categoryId = product.CategoryId,
                categoryName = product.Category?.Name ?? "ไม่มีหมวดหมู่"
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            try
            {
                // ตรวจสอบว่ามี Category หรือไม่
                if (product.CategoryId > 0)
                {
                    var category = await _context.Categories.FindAsync(product.CategoryId);
                    if (category == null)
                    {
                        return Json(new { success = false, message = "ไม่พบหมวดหมู่ที่ระบุ" });
                    }
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return Json(new { success = true, product });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] Product product)
        {
            try
            {
                // ตรวจสอบว่ามี Category หรือไม่
                if (product.CategoryId > 0)
                {
                    var category = await _context.Categories.FindAsync(product.CategoryId);
                    if (category == null)
                    {
                        return Json(new { success = false, message = "ไม่พบหมวดหมู่ที่ระบุ" });
                    }
                }

                var existingProduct = await _context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // อัพเดทข้อมูล
                existingProduct.Name = product.Name;
                existingProduct.Barcode = product.Barcode;
                existingProduct.Price = product.Price;
                existingProduct.Stock = product.Stock;
                existingProduct.CategoryId = product.CategoryId;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound();

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // สำหรับเพิ่มหมวดหมู่

        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] Category category)
        {
            if (string.IsNullOrEmpty(category.Name))
            {
                return Json(new { success = false, message = "กรุณาระบุชื่อหมวดหมู่" });
            }

            try
            {
                // ตรวจสอบว่ามีชื่อหมวดหมู่ซ้ำหรือไม่
                var exists = await _context.Categories
                    .AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());

                if (exists)
                {
                    return Json(new { success = false, message = "มีชื่อหมวดหมู่นี้อยู่แล้ว" });
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    category = new
                    {
                        id = category.Id,
                        name = category.Name
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory([FromBody] Category category)
        {
            if (string.IsNullOrEmpty(category.Name))
            {
                return Json(new { success = false, message = "กรุณาระบุชื่อหมวดหมู่" });
            }

            try
            {
                var existingCategory = await _context.Categories.FindAsync(category.Id);
                if (existingCategory == null)
                {
                    return Json(new { success = false, message = "ไม่พบหมวดหมู่ที่ต้องการแก้ไข" });
                }

                // ตรวจสอบชื่อซ้ำ (ยกเว้นชื่อเดิมของตัวเอง)
                var duplicateName = await _context.Categories
                    .AnyAsync(c => c.Id != category.Id &&
                                  c.Name.ToLower() == category.Name.ToLower());

                if (duplicateName)
                {
                    return Json(new { success = false, message = "มีชื่อหมวดหมู่นี้อยู่แล้ว" });
                }

                existingCategory.Name = category.Name;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "ไม่พบหมวดหมู่ที่ต้องการลบ" });
                }

                // ตรวจสอบว่ามีสินค้าในหมวดหมู่หรือไม่
                if (category.Products.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = $"ไม่สามารถลบหมวดหมู่ได้เนื่องจากมีสินค้าอยู่ {category.Products.Count} รายการ"
                    });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}