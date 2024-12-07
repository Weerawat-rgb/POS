using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Web.Data;
using POS.Web.Models.Entities;

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
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductCount = c.Products.Count(p => p.IsActive && p.Status)
                })
                .ToListAsync();

            ViewBag.Products = await _context.Products
                .Where(p => p.IsActive && p.Status)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    Price = p.Price,
                    Stock = p.Stock,
                    ImageBase64 = p.ImageBase64,
                    ImageType = p.ImageType
                })
                .Take(12) // แสดง 12 รายการแรก
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

        // [HttpGet("SearchProducts/{searchTerm?}")]
        // public async Task<IActionResult> SearchProducts(string searchTerm)
        // {
        //     try
        //     {
        //         // สร้างเลขที่ใบเสร็จ
        //         sale.InvoiceNumber = GenerateInvoiceNumber();
        //         sale.SaleDate = DateTime.Now;

        //         _context.Sales.Add(sale);
        //         await _context.SaveChangesAsync();

        //         // อัพเดต stock
        //         foreach (var detail in sale.SaleDetails)
        //         {
        //             var product = await _context.Products.FindAsync(detail.ProductId);
        //             if (product != null)
        //             {
        //                 product.Stock -= detail.Quantity;
        //                 _context.Products.Update(product);
        //             }
        //         }
        //         await _context.SaveChangesAsync();

        //         return Json(new { success = true, saleId = sale.Id });
        //     }
        //     catch (Exception ex)
        //     {
        //         return Json(new { success = false, message = ex.Message });
        //     }
        // // }

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


        // private string GenerateInvoiceNumber()
        // {
        //     var today = DateTime.Now;
        //     var prefix = $"INV{today:yyMMdd}";
        //     var lastInvoice = _context.Sales
        //         .Where(s => s.InvoiceNumber.StartsWith(prefix))
        //         .OrderByDescending(s => s.InvoiceNumber)
        //         .Select(s => s.InvoiceNumber)
        //         .FirstOrDefault();

        //     int sequence = 1;
        //     if (lastInvoice != null)
        //     {
        //         var lastSequence = int.Parse(lastInvoice.Substring(prefix.Length));
        //         sequence = lastSequence + 1;
        //     }

        //     return $"{prefix}{sequence:D4}";
        // }

        [HttpGet("SearchProducts/{searchTerm?}")]
        public async Task<IActionResult> SearchProducts(string searchTerm)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.Status &&
                        (p.Barcode.Contains(searchTerm) ||
                         p.Name.Contains(searchTerm)))
                    .Take(10)
                    .Select(p => new
                    {
                        id = p.Id,
                        barcode = p.Barcode ?? "",
                        name = p.Name ?? "",
                        price = p.Price,
                        stock = p.Stock,
                        categoryName = p.Category != null ? p.Category.Name : "ไม่ระบุหมวดหมู่",  // ตรวจสอบ null ก่อนเข้าถึง Name
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

        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.IsActive && p.Status)
                    .Select(p => new
                    {
                        id = p.Id,
                        barcode = p.Barcode,
                        name = p.Name,
                        price = p.Price,
                        stock = p.Stock,
                        imageBase64 = p.ImageBase64,
                        imageType = p.ImageType
                    })
                    .ToListAsync();

                return Json(new { success = true, products });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

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
                        productCount = _context.Products.Count(p => p.CategoryId == c.Id && p.IsActive && p.Status)
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
                    .Where(p => p.CategoryId == categoryId && p.IsActive && p.Status)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        price = p.Price,
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
    }

}