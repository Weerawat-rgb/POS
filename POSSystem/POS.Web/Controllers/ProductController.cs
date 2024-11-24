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
                .OrderBy(p => p.Category != null ? p.Category.Name : string.Empty)  // จัดการกรณี Category เป็น null
                .ThenBy(p => p.Name)
                .ToListAsync();

            var categories = await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.IsActive)  // เพิ่มเงื่อนไขดึงเฉพาะ active categories
                .OrderBy(c => c.Name)
                .Select(c => new  // เพิ่ม Select เพื่อรวมข้อมูลจำนวนสินค้า
                {
                    Category = c,
                    ProductCount = c.Products.Count
                })
                .ToListAsync();

            ViewBag.Categories = categories.Select(x => x.Category).ToList();
            ViewBag.ProductCounts = categories.ToDictionary(x => x.Category.Id, x => x.ProductCount);

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories(bool showDeleted = false)
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .Where(c => c.IsActive != showDeleted)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        productCount = c.Products.Count
                    })
                    .OrderBy(c => c.name)
                    .ToListAsync();

                return Json(categories);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
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
        public async Task<IActionResult> Create([FromForm] Product product, IFormFile? imageFile)
        {
            try
            {
                if (imageFile != null)
                {
                    // บันทึกรูปภาพ
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                    var imagePath = Path.Combine("wwwroot", "images", "products", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);

                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    product.Image = $"/images/products/{fileName}";
                }

                product.IsActive = true;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
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
                // ตรวจสอบข้อมูล
                if (string.IsNullOrEmpty(product.Name))
                {
                    return Json(new { success = false, message = "กรุณาระบุชื่อสินค้า" });
                }

                if (string.IsNullOrEmpty(product.Barcode))
                {
                    return Json(new { success = false, message = "กรุณาระบุบาร์โค้ด" });
                }

                // ตรวจสอบบาร์โค้ดซ้ำ (ยกเว้นรายการปัจจุบัน)
                var existingBarcode = await _context.Products
                    .AnyAsync(p => p.Barcode == product.Barcode && p.Id != product.Id);

                if (existingBarcode)
                {
                    return Json(new { success = false, message = "บาร์โค้ดนี้มีในระบบแล้ว" });
                }

                // ตรวจสอบหมวดหมู่
                if (product.CategoryId == 0)
                {
                    return Json(new { success = false, message = "กรุณาเลือกหมวดหมู่" });
                }

                var category = await _context.Categories.FindAsync(product.CategoryId);
                if (category == null)
                {
                    return Json(new { success = false, message = "ไม่พบหมวดหมู่ที่เลือก" });
                }

                // ดึงข้อมูลเดิม
                var existingProduct = await _context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    return Json(new { success = false, message = "ไม่พบสินค้าที่ต้องการแก้ไข" });
                }

                // อัพเดตข้อมูล
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
                return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
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

        // Add Categories

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

                // ส่งข้อมูลที่จำเป็นกลับไป
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

        // Update Categories
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

        // Delete Categories
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)  // ลบ [FromBody] ออก
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

                // Soft delete
                category.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // เพิ่มเมธอดสำหรับกู้คืนหมวดหมู่
        [HttpPost]
        public async Task<IActionResult> RestoreCategory([FromBody] int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "ไม่พบหมวดหมู่" });
                }

                // ตรวจสอบว่ามีชื่อซ้ำกับหมวดหมู่ที่ active อยู่หรือไม่
                var duplicateName = await _context.Categories
                    .AnyAsync(c => c.IsActive && c.Name == category.Name);

                if (duplicateName)
                {
                    return Json(new
                    {
                        success = false,
                        message = "มีหมวดหมู่ที่ใช้งานอยู่ใช้ชื่อนี้แล้ว"
                    });
                }

                category.IsActive = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name
                    })
                    .OrderBy(c => c.name)
                    .ToListAsync();

                return Json(categories);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("GetProduct/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound();
                }

                return Json(new
                {
                    id = product.Id,
                    name = product.Name,
                    barcode = product.Barcode,
                    price = product.Price,
                    stock = product.Stock,
                    categoryId = product.CategoryId,
                    categoryName = product.Category?.Name
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}