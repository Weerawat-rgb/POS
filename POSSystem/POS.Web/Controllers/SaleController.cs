using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Web.Data;
using POS.Web.Models.Entities;

namespace POS.Web.Controllers
{
    public class SaleController : Controller
    {
        private readonly POSDbContext _context;

        public SaleController(POSDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            if (product == null)
                return NotFound();

            return Json(new
            {
                id = product.Id,
                name = product.Name,
                price = product.Price,
                stock = product.Stock,
                categoryName = product.Category?.Name
            });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessSale([FromBody] Sale sale)
        {
            try
            {
                // สร้างเลขที่ใบเสร็จ
                sale.InvoiceNumber = GenerateInvoiceNumber();
                sale.SaleDate = DateTime.Now;

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                // อัพเดต stock
                foreach (var detail in sale.SaleDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.Stock -= detail.Quantity;
                        _context.Products.Update(product);
                    }
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, saleId = sale.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpGet]
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


        private string GenerateInvoiceNumber()
        {
            var today = DateTime.Now;
            var prefix = $"INV{today:yyMMdd}";
            var lastInvoice = _context.Sales
                .Where(s => s.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(s => s.InvoiceNumber)
                .Select(s => s.InvoiceNumber)
                .FirstOrDefault();

            int sequence = 1;
            if (lastInvoice != null)
            {
                var lastSequence = int.Parse(lastInvoice.Substring(prefix.Length));
                sequence = lastSequence + 1;
            }

            return $"{prefix}{sequence:D4}";
        }
    }
}