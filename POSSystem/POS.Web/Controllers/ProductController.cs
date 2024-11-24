using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Web.Data;
using POS.Web.Models.Entities;
using OfficeOpenXml;
using System.Text;

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
                .Where(p => p.IsActive)  // ดึงเฉพาะรายการที่ยังไม่ถูกลบ
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

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] Product model, IFormFile? imageFile)
        {
            try
            {
                // ตรวจสอบข้อมูล
                if (string.IsNullOrEmpty(model.Name))
                    return Json(new { success = false, message = "กรุณาระบุชื่อสินค้า" });

                if (string.IsNullOrEmpty(model.Barcode))
                    return Json(new { success = false, message = "กรุณาระบุบาร์โค้ด" });

                if (model.CategoryId <= 0)
                    return Json(new { success = false, message = "กรุณาเลือกหมวดหมู่" });

                // ตรวจสอบบาร์โค้ดซ้ำ
                var existingBarcode = await _context.Products
                    .AnyAsync(p => p.Barcode == model.Barcode && p.IsActive);

                if (existingBarcode)
                    return Json(new { success = false, message = "บาร์โค้ดนี้มีในระบบแล้ว" });

                var product = new Product
                {
                    Name = model.Name,
                    Barcode = model.Barcode,
                    Price = model.Price,
                    Stock = model.Stock,
                    CategoryId = model.CategoryId,
                    Status = true,
                    IsActive = true
                };

                // จัดการรูปภาพ
                if (imageFile != null)
                {
                    using var memoryStream = new MemoryStream();
                    await imageFile.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    product.ImageBase64 = Convert.ToBase64String(imageBytes);
                    product.ImageType = imageFile.ContentType;
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "เพิ่มสินค้าเรียบร้อย" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromForm] Product product, IFormFile? imageFile)
        {
            try
            {
                var existingProduct = await _context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    return Json(new { success = false, message = "ไม่พบสินค้า" });
                }

                // ตรวจสอบบาร์โค้ดซ้ำ (ยกเว้นตัวเอง)
                var existingBarcode = await _context.Products
                    .AnyAsync(p => p.Barcode == product.Barcode
                                  && p.Id != product.Id
                                  && p.IsActive);

                if (existingBarcode)
                {
                    return Json(new { success = false, message = "บาร์โค้ดนี้มีในระบบแล้ว" });
                }

                // จัดการรูปภาพ
                if (imageFile != null)
                {
                    using var memoryStream = new MemoryStream();
                    await imageFile.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    existingProduct.ImageBase64 = Convert.ToBase64String(imageBytes);
                    existingProduct.ImageType = imageFile.ContentType;
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
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "ไม่พบสินค้า" });
                }

                product.IsActive = false;  // Soft delete
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "ลบสินค้าเรียบร้อย" });
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

        [HttpGet("Product/GetProduct/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Json(new { success = false, message = "ไม่พบสินค้า" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = product.Id,
                        categoryId = product.CategoryId,
                        name = product.Name,
                        barcode = product.Barcode,
                        price = product.Price,
                        stock = product.Stock,
                        status = product.Status,
                        imageBase64 = product.ImageBase64,
                        imageType = product.ImageType
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "ไม่พบสินค้า" });
                }

                product.Status = !product.Status;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    newStatus = product.Status,
                    message = product.Status ? "เปิดการขายสินค้าแล้ว" : "ปิดการขายสินค้าชั่วคราว"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Export and Import
        [HttpGet]
        public IActionResult ExportTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Sheet 1: Template
                var worksheet = package.Workbook.Worksheets.Add("Template");

                // สร้างหัวตาราง เรียงลำดับใหม่
                var headers = new[] { "ชื่อหมวด*", "Barcode*", "ชื่อสินค้า*", "ราคา*", "จำนวน*" };
                for (int i = 0; i < headers.Length; i++)
                {
                    // ใส่หัวตาราง
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];

                    // จัดแต่งหัวตาราง
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // ดึงข้อมูลหมวดหมู่
                var categories = _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToList();


                // เพิ่มข้อมูลตัวอย่าง
                var sampleData = new[]
                {
                    new { CategoryName = "เครื่องดื่ม", Barcode = "8850999111111", Name = "น้ำอัดลม cola", Price = 15.00m, Stock = 100 },
                    new { CategoryName = "อาหาร", Barcode = "8850999222222", Name = "มาม่า ต้มยำ", Price = 6.00m, Stock = 200 },
                    new { CategoryName = "ขนม", Barcode = "8850999333333", Name = "เลย์ รสออริจินัล", Price = 20.00m, Stock = 50 },
                    new { CategoryName = "เครื่องดื่ม", Barcode = "8850999444444", Name = "น้ำดื่ม", Price = 7.00m, Stock = 500 }
                };

                // ใส่ข้อมูลตัวอย่าง
                for (int i = 0; i < sampleData.Length; i++)
                {
                    var row = i + 2;
                    var data = sampleData[i];
                    worksheet.Cells[row, 1].Value = data.CategoryName;  // ชื่อหมวด
                    worksheet.Cells[row, 2].Value = data.Barcode;      // Barcode
                    worksheet.Cells[row, 3].Value = data.Name;         // ชื่อสินค้า
                    worksheet.Cells[row, 4].Value = data.Price;        // ราคา
                    worksheet.Cells[row, 5].Value = data.Stock;        // จำนวน
                }

                // จัดแต่งข้อมูลตัวอย่าง
                var dataRange = worksheet.Cells[2, 1, sampleData.Length + 1, headers.Length];
                dataRange.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                dataRange.Style.Font.Color.SetColor(System.Drawing.Color.Gray); // ทำให้ตัวอย่างเป็นสีเทา

                // Sheet 2: Categories Reference
                var categorySheet = package.Workbook.Worksheets.Add("รายการหมวดหมู่");
                categorySheet.Cells["A1"].Value = "รหัสหมวดหมู่";
                categorySheet.Cells["B1"].Value = "ชื่อหมวดหมู่";

                // จัดแต่งหัวตารางหมวดหมู่
                var categoryHeader = categorySheet.Cells["A1:B1"];
                categoryHeader.Style.Font.Bold = true;
                categoryHeader.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                categoryHeader.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                categoryHeader.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                // เพิ่มข้อมูลหมวดหมู่
                for (int i = 0; i < categories.Count; i++)
                {
                    var row = i + 2;
                    categorySheet.Cells[row, 1].Value = categories[i].Id;
                    categorySheet.Cells[row, 2].Value = categories[i].Name;
                }

                // Sheet 3: คำแนะนำ
                var guideSheet = package.Workbook.Worksheets.Add("คำแนะนำ");
                var guidelines = new[]
                {
            "คำแนะนำการใช้งานไฟล์ Template:",
            "1. ช่องที่มีเครื่องหมาย * จำเป็นต้องกรอกข้อมูล",
            "2. ชื่อหมวด: ระบุชื่อหมวดหมู่ตามที่มีในระบบ (ดูได้จาก sheet 'รายการหมวดหมู่')",
            "3. Barcode: ระบุรหัสบาร์โค้ดสินค้า (ไม่ซ้ำกัน)",
            "4. ชื่อสินค้า: ระบุชื่อสินค้า",
            "5. ราคา: ระบุราคาขาย (ตัวเลขทศนิยม 2 ตำแหน่ง)",
            "6. จำนวน: ระบุจำนวนสินค้าคงเหลือ (ตัวเลขจำนวนเต็ม)",
            "",
            "หมายเหตุ:",
            "- ข้อมูลในแถวสีเทาเป็นตัวอย่าง สามารถลบออกได้",
            "- หากระบุ Barcode ที่มีอยู่แล้ว ระบบจะอัพเดตข้อมูลสินค้านั้น",
            "- รายการหมวดหมู่ทั้งหมดสามารถดูได้จาก sheet 'รายการหมวดหมู่'"
        };

                // ใส่คำแนะนำ
                for (int i = 0; i < guidelines.Length; i++)
                {
                    guideSheet.Cells[i + 1, 1].Value = guidelines[i];
                    if (i == 0) // หัวข้อ
                    {
                        guideSheet.Cells[i + 1, 1].Style.Font.Bold = true;
                        guideSheet.Cells[i + 1, 1].Style.Font.Size = 14;
                    }
                }

                // จัดความกว้างของคอลัมน์
                worksheet.Cells.AutoFitColumns();
                categorySheet.Cells.AutoFitColumns();
                guideSheet.Cells.AutoFitColumns();

                // สร้าง Dropdown list สำหรับชื่อหมวดหมู่
                var validation = worksheet.DataValidations.AddListValidation("A2:A1000");
                validation.Formula.ExcelFormula = $"=รายการหมวดหมู่!$B$2:$B${categories.Count + 1}";

                // สร้างไฟล์
                var content = package.GetAsByteArray();
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "ProductImportTemplate.xlsx";

                return File(content, contentType, fileName);
            }
        }
        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length <= 0)
                {
                    return Json(new { success = false, message = "กรุณาเลือกไฟล์" });
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(file.OpenReadStream()))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;
                    var errors = new List<string>();
                    var importedCount = 0;
                    var updatedCount = 0;
                    var newCategoryCount = 0;

                    // ดึงข้อมูลหมวดหมู่ที่มีอยู่
                    var existingCategories = await _context.Categories
                        .Where(c => c.IsActive)
                        .ToDictionaryAsync(c => c.Name.Trim().ToLower(), c => c);

                    // เริ่มจากแถวที่ 2 (ข้ามส่วนหัว)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var categoryName = worksheet.Cells[row, 1].Value?.ToString()?.Trim();  // ชื่อหมวด
                            var barcode = worksheet.Cells[row, 2].Value?.ToString()?.Trim();       // Barcode
                            var name = worksheet.Cells[row, 3].Value?.ToString()?.Trim();          // ชื่อสินค้า
                            var priceStr = worksheet.Cells[row, 4].Value?.ToString()?.Trim();      // ราคา
                            var stockStr = worksheet.Cells[row, 5].Value?.ToString()?.Trim();      // จำนวน

                            // ข้ามแถวว่าง
                            if (string.IsNullOrEmpty(barcode) && string.IsNullOrEmpty(name)) continue;

                            // ตรวจสอบข้อมูลจำเป็น
                            if (string.IsNullOrEmpty(categoryName))
                            {
                                errors.Add($"แถวที่ {row}: กรุณาระบุชื่อหมวดหมู่");
                                continue;
                            }

                            if (string.IsNullOrEmpty(barcode))
                            {
                                errors.Add($"แถวที่ {row}: กรุณาระบุบาร์โค้ด");
                                continue;
                            }

                            if (string.IsNullOrEmpty(name))
                            {
                                errors.Add($"แถวที่ {row}: กรุณาระบุชื่อสินค้า");
                                continue;
                            }

                            // แปลงข้อมูลราคา
                            if (!decimal.TryParse(priceStr, out decimal price))
                            {
                                errors.Add($"แถวที่ {row}: ราคาไม่ถูกต้อง");
                                continue;
                            }

                            // แปลงข้อมูลจำนวน
                            if (!int.TryParse(stockStr, out int stock))
                            {
                                errors.Add($"แถวที่ {row}: จำนวนไม่ถูกต้อง");
                                continue;
                            }

                            // ตรวจสอบค่าติดลบ
                            if (price < 0)
                            {
                                errors.Add($"แถวที่ {row}: ราคาต้องไม่ติดลบ");
                                continue;
                            }

                            if (stock < 0)
                            {
                                errors.Add($"แถวที่ {row}: จำนวนต้องไม่ติดลบ");
                                continue;
                            }

                            // ตรวจสอบและสร้างหมวดหมู่ถ้าไม่มี
                            Category category;
                            var categoryKey = categoryName.ToLower();
                            if (!existingCategories.TryGetValue(categoryKey, out category))
                            {
                                category = new Category
                                {
                                    Name = categoryName,
                                    IsActive = true
                                };
                                _context.Categories.Add(category);
                                await _context.SaveChangesAsync(); // บันทึกเพื่อให้ได้ Id
                                existingCategories.Add(categoryKey, category);
                                newCategoryCount++;
                            }

                            // ตรวจสอบสินค้าจาก barcode
                            var existingProduct = await _context.Products
                                .FirstOrDefaultAsync(p => p.Barcode == barcode);

                            if (existingProduct != null)
                            {
                                // อัพเดตข้อมูลที่มีอยู่
                                existingProduct.Name = name;
                                existingProduct.Price = price;
                                existingProduct.Stock = stock;
                                existingProduct.CategoryId = category.Id;
                                existingProduct.IsActive = true; // เปิดใช้งานถ้าเคยถูกลบ
                                updatedCount++;
                            }
                            else
                            {
                                // เพิ่มสินค้าใหม่
                                var product = new Product
                                {
                                    Barcode = barcode,
                                    Name = name,
                                    Price = price,
                                    Stock = stock,
                                    CategoryId = category.Id,
                                    Status = true,
                                    IsActive = true
                                };
                                _context.Products.Add(product);
                                importedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"แถวที่ {row}: {ex.Message}");
                        }
                    }

                    await _context.SaveChangesAsync();

                    // สรุปผล
                    var result = new StringBuilder();
                    result.AppendLine($"สรุปการนำเข้า:");
                    result.AppendLine($"- เพิ่มหมวดหมู่ใหม่: {newCategoryCount} รายการ");
                    result.AppendLine($"- เพิ่มสินค้าใหม่: {importedCount} รายการ");
                    result.AppendLine($"- อัพเดตสินค้า: {updatedCount} รายการ");

                    if (errors.Any())
                    {
                        result.AppendLine($"\nพบข้อผิดพลาด {errors.Count} รายการ:");
                        result.AppendLine(string.Join(Environment.NewLine, errors));
                    }

                    return Json(new
                    {
                        success = true,
                        newCategories = newCategoryCount,
                        newProducts = importedCount,
                        updatedProducts = updatedCount,
                        errorCount = errors.Count,
                        message = result.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }
    }
}