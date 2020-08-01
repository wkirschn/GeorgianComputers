using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeorgianComputers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace GeorgianComputers.Controllers
{
    public class ShopController : Controller
    {


        //add db connection

        private readonly GeorgianComputersContext _context; //Will be different for my site, note the underscore


        public ShopController(GeorgianComputersContext context)
        {
            _context = context;
        }


        //GET: /Shop
        public IActionResult Index()
        {

            //return list of Cateogires for the user to browse
            var categories = _context.Category.OrderBy(c => c.Name).ToList();


            return View(categories);
        }

        //GET:  /browse/cartName
        public IActionResult Browse(string category)
        {

            // Store the selected category name in the ViewBag so we can display in the View Heading

            ViewBag.Category = category;


            // Get the list of products for the selected category and pass the list to the view



            var products = _context.Product.Where(p => p.Category.Name == category).OrderBy(p => p.Name).ToList();
            return View(products);


        }

        //Get: /ProductDetails/prodName

        public IActionResult ProductDetails(string product)
        {
            // Use a SingleOrDefault to find either 1 exact match or a null object

            var selectedProduct = _context.Product.SingleOrDefault(p => p.Name == product);
            return View(selectedProduct);
        }

        //POST: AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult AddToCart(int Quantity, int ProductId)
        {
            //Identify product Price
            var product = _context.Product.SingleOrDefault(p => p.ProductId == ProductId);
            var price = product.Price;
            // Determine Username, since it is currently hard coded

            var cartUsername = GetCartUserName();

            // Check if THIS USER's product already exists in the cart. If so, update the quantity

            var cartItem = _context.Cart.SingleOrDefault(c => c.ProductId == ProductId && c.Username == cartUsername);
            if (cartItem == null)
            {

            var cart = new Cart
            {
                ProductId = ProductId,
                Quantity = Quantity,
                Price = price,
                Username = cartUsername
            };
                // Create and save a new Cart Object
                _context.Cart.Add(cart);
            }

            else
            {
                cartItem.Quantity += Quantity; // Add the new quantity to the existing quantity
                _context.Update(cartItem);
            }
            
            _context.SaveChanges();

            return RedirectToAction("Cart");
        }

        //Check or set Cart username

        private string GetCartUserName()
        {
            //1. Check if we already are stored in the Username in the user's session?
            if (HttpContext.Session.GetString("CartUsername") == null)
            {

                //Initialize an empty string variable that will later add to the session object
                var cartUsername = "";


                //2. If no, Username in session thare are no items in the cart yet, is user logged in?
                // If yes, use their email for the session variable
                if(User.Identity.IsAuthenticated)
                {
                    cartUsername = User.Identity.Name;
                }

                else
                {
                    // If no, use the GUID to make a new ID and stoire that in the session
                    cartUsername = Guid.NewGuid().ToString();
                }




                // Next, store the cartUsername in a session var
                HttpContext.Session.SetString("cartUsername", cartUsername);

            }
            // Send back the Username
            return HttpContext.Session.GetString("cartUsername");
        }


        public IActionResult Cart()
        {
            // Figure out who the user is

            var cartUsername = GetCartUserName();


            // Query the DB to get the user's cart items

            var cartItems = _context.Cart.Include(c => c.Product).Where(c => c.Username == cartUsername).ToList();

            // Load a view to pass the cart items for display

            return View(cartItems);



        }

        public IActionResult RemoveFromCart(int id)
        {
            // get the object the user wants to delete
            var cartItem = _context.Cart.SingleOrDefault(c => c.CartId == id);
            // delete the object

            _context.Cart.Remove(cartItem);
            _context.SaveChanges();

            //redirect to updated cart page where deleteed item is gone
            return RedirectToAction("Cart");
        }

        [Authorize]
        public IActionResult Checkout() // SETTER
        {
            // Check if the user has been shopping anonymously now that they are logged in
            MigrateCart();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Checkout([Bind("FirstName, LastName, Address, City, Province, PostalCode, Phone")] Order order) // GET
        {
            // Autofill the date, user, and total properties instead of the user doing this
            order.OrderDate = DateTime.Now;
            order.UserId = User.Identity.Name;

            var cartItems = _context.Cart.Where(c => c.Username == User.Identity.Name);
            decimal cartTotal = (from c in cartItems select c.Quantity * c.Price).Sum();

            order.Total = cartTotal;


            // Will NEED and EXTENSION to the .Net Core Session object to store the order Object - in the next video!
            HttpContext.Session.SetString("cartTotal", cartTotal.ToString());

            return RedirectToAction("Payment");
        }


        private void MigrateCart()
        {

            // If the user has shopped without an account, attach their items to their username

            if(HttpContext.Session.GetString("CartUsername") != User.Identity.Name)
            {
                var cartUsername = HttpContext.Session.GetString("CartUsername");
                // Get the user's cart items
                var cartItems = _context.Cart.Where(c => c.Username == cartUsername);
                // Loop through the cart items and update the username for each one

                foreach(var item in cartItems)
                {
                    item.Username = User.Identity.Name;
                    _context.Update(item);
                }
                _context.SaveChanges();

                // Update the session variable from a GUID to the user's email

                HttpContext.Session.SetString("CartUsername", User.Identity.Name);
            }

        }


        public IActionResult Payment()
        {
            return View();
        }








    }



}
