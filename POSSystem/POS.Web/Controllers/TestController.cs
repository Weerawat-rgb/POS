using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using POS.Web.Data;

namespace POS.Web.Controllers
{
    public class TestController : Controller
    {
        private readonly POSDbContext _context;
        private readonly IConfiguration _configuration;

        public TestController(POSDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult TestDB()
        {
            try
            {
                // ทดสอบการเชื่อมต่อ
                using (var connection = new SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    ViewBag.Status = "เชื่อมต่อสำเร็จ!";

                    // นับจำนวนสินค้า
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM Products", connection))
                    {
                        int count = (int)command.ExecuteScalar();
                        ViewBag.ProductCount = count;
                    }

                    // ดึงข้อมูลสินค้า
                    using (var command = new SqlCommand(@"
                        SELECT TOP 5 p.*, c.Name as CategoryName 
                        FROM Products p
                        LEFT JOIN Categories c ON p.CategoryId = c.Id", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            var products = new List<dynamic>();
                            while (reader.Read())
                            {
                                products.Add(new
                                {
                                    Id = reader["Id"],
                                    Name = reader["Name"],
                                    Price = reader["Price"],
                                    CategoryName = reader["CategoryName"]
                                });
                            }
                            ViewBag.Products = products;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Status = $"เกิดข้อผิดพลาด: {ex.Message}";
            }

            return View();
        }
    }
}