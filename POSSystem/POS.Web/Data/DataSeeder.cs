using POS.Web.Models.Entities;

namespace POS.Web.Data
{
    public static class DataSeeder
    {
        public static void Initialize(POSDbContext context)
        {
            context.Database.EnsureCreated();

            // ตรวจสอบว่ามีข้อมูลหมวดหมู่หรือยัง
            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category { Name = "เครื่องดื่ม" },
                    new Category { Name = "ขนม" },
                    new Category { Name = "อาหาร" }
                };

                context.Categories.AddRange(categories);
                context.SaveChanges();
            }

            // ตรวจสอบว่ามีข้อมูลสินค้าหรือยัง
            if (!context.Products.Any())
            {
                var products = new Product[]
                {
                    new Product { 
                        Name = "น้ำอัดลม cola", 
                        Barcode = "8850999111111",
                        Price = 15,
                        Stock = 100,
                        CategoryId = 1
                    },
                    new Product {
                        Name = "มาม่า ต้มยำ",
                        Barcode = "8850999222222",
                        Price = 6,
                        Stock = 200,
                        CategoryId = 3
                    },
                    new Product {
                        Name = "เลย์ รสออริจินัล",
                        Barcode = "8850999333333",
                        Price = 20,
                        Stock = 50,
                        CategoryId = 2
                    }
                };

                context.Products.AddRange(products);
                context.SaveChanges();
            }
        }
    }
}