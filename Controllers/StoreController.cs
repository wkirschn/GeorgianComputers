using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GeorgianComputers.Models;

namespace GeorgianComputers.Controllers
{
    public class StoreController : Controller
    {
        public IActionResult Index()
        {
            var products = new List<Product>();
            //We will use a for loop to populate the product list
            for(var i = 1; i <= 10; i++)
            {
                products.Add(new Product { Name = "Product " + i.ToString() });
            }


            return View(products);
        }

        public IActionResult Details(string product)
        {
            //Store the selected product name in the ViewBag container
            //ViewBag is automatically passed back to the view that loads

            ViewBag.product = product;
            return View();
        }
    }
}
