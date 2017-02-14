//css_inc webapi.cs
//css_npp asadmin;

using System;
using System.Linq;
using System.Web.Http;
using System.Collections.Generic;
using WebApi;
using System.Net;

class Script
{
    static void Main()
    {
        SimpleHost.StartAsConosle("http://localhost:8082",
                                   server =>
                                   {
                                       Console.WriteLine("Press Enter to quit.");
                                       Console.WriteLine("---------------------");
                                       server.Configuration.OutputRouts(Console.WriteLine);
                                       Console.WriteLine("---------------------");
                                       Console.ReadLine();
                                   });
    }
}

namespace WebAPI_Sample
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
    }

    public class ProductsController : ApiController
    {
        Product[] products = new Product[]
        {
            new Product { Id = 1, Name = "Tomato Soup", Category = "Groceries", Price = 1 },
            new Product { Id = 2, Name = "Yo-yo", Category = "Toys", Price = 3.75M },
            new Product { Id = 3, Name = "Hammer", Category = "Hardware", Price = 16.99M }
        };

        public IEnumerable<Product> GetAllProducts()
        {
            Console.WriteLine("GetAllProducts()");
            return products;
        }

        public Product GetProductById(int id)
        {
            var product = products.FirstOrDefault((p) => p.Id == id);

            if (product == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            return product;
        }

        public IEnumerable<Product> GetProductsByCategory(string category)
        {
            return products.Where(p => string.Equals(p.Category, category,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}