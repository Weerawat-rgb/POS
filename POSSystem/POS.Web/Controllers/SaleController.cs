using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Web.Data;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SaleController(POSDbContext context) : Controller
    {
        private readonly POSDbContext _context = context;
        private readonly object? sale;

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductCount = c.Products.Count(p => p.IsActive )
                })
                .ToListAsync();

            ViewBag.Categories = categories;

            var firstCategoryId = categories.FirstOrDefault()?.Id;

            ViewBag.Products = await _context.Products
                .Where(p => p.IsActive &&
                            // p.Status &&
                            p.CategoryId == firstCategoryId)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    Price = p.Price,
                    Stock = p.Stock,
                    Status = p.Status,
                    ImageBase64 = p.ImageBase64,
                    ImageType = p.ImageType
                })
                .ToListAsync();

            return View();
        }

        [HttpGet("GetProductByBarcode/{barcode}")]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);

                if (product == null)
                {
                    return Json(new { success = false, message = "ไม่พบรหัสสินค้านี้" });
                }

                if (!product.IsActive)
                {
                    return Json(new { success = false, message = "สินค้าถูกปิดการใช้งาน" });
                }

                if (!product.Status)
                {
                    return Json(new { success = false, message = "สินค้าถูกปิดการขาย" });
                }

                if (product.Stock <= 0)
                {
                    return Json(new { success = false, message = "สินค้าหมด" });
                }

                return Json(new
                {
                    success = true,
                    product = new
                    {
                        id = product.Id,
                        barcode = product.Barcode,
                        name = product.Name,
                        price = product.Price,
                        stock = product.Stock,
                        categoryName = product.Category?.Name,
                        imageBase64 = product.ImageBase64,
                        imageType = product.ImageType
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }


        [HttpGet("PrintReceipt/{id}")]
        public async Task<IActionResult> PrintReceipt(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
                return NotFound();

            return View(sale);
        }


        [HttpGet("SearchProducts")]
        public async Task<IActionResult> SearchProducts([FromQuery] string? searchTerm = "")
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive &&
                        (string.IsNullOrEmpty(searchTerm) ||
                         p.Barcode.Contains(searchTerm) ||
                         p.Name.Contains(searchTerm)))
                    .Take(10)
                    .Select(p => new
                    {
                        id = p.Id,
                        barcode = p.Barcode ?? "",
                        name = p.Name ?? "",
                        price = p.Price,
                        status = p.Status,
                        stock = p.Stock,
                        categoryName = p.Category != null ? p.Category.Name : "ไม่ระบุหมวดหมู่",
                        imageBase64 = p.ImageBase64 ?? "",
                        imageType = p.ImageType ?? ""
                    })
                    .ToListAsync();

                return Json(new { success = true, products });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // [HttpGet("GetAllProducts")]
        // public async Task<IActionResult> GetAllProducts()
        // {
        //     try
        //     {
        //         var products = await _context.Products
        //             .Where(p => p.IsActive && p.Status)
        //             .Select(p => new
        //             {
        //                 id = p.Id,
        //                 barcode = p.Barcode,
        //                 name = p.Name,
        //                 price = p.Price,
        //                 stock = p.Stock,
        //                 imageBase64 = p.ImageBase64,
        //                 imageType = p.ImageType
        //             })
        //             .ToListAsync();

        //         return Json(new { success = true, products });
        //     }
        //     catch (Exception ex)
        //     {
        //         return Json(new { success = false, message = ex.Message });
        //     }
        // }

        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        productCount = _context.Products.Count(p => p.CategoryId == c.Id && p.IsActive)
                    })
                    .ToListAsync();

                return Json(new { success = true, categories });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpGet("GetProductsByCategory/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.CategoryId == categoryId && p.IsActive)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        price = p.Price,
                        status = p.Status,
                        stock = p.Stock,
                        imageBase64 = p.ImageBase64,
                        imageType = p.ImageType,
                        barcode = p.Barcode,
                        // เพิ่มข้อมูลอื่นๆ ที่จำเป็นสำหรับ addToCart
                    })
                    .ToListAsync();

                return Json(new { success = true, products });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetProductById")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Where(p => p.Id == id && p.IsActive && p.Status)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return Json(product);
        }

    }



}