using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeorgianComputers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace GeorgianComputers.Controllers
{
    public class ShopController : Controller
    {


        //add db connection

        private readonly GeorgianComputersContext _context; //Will be different for my site, note the underscore

        //Add configuration so controller can read config value appsettings.json
        private IConfiguration _configuration;




        public ShopController(GeorgianComputersContext context, IConfiguration configuration) // Dependenacy Injection
        {
            //accept an instance of our DB connection class and use this object connection, underscore means an object that is being injected, similar to this.

            _context = context;

            // accept an instance of the configuration object so we can read appsetting.json

            _configuration = configuration;


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

        public IActionResult Checkout([Bind("FirstName, LastName, Address, City, Province, PostalCode, Phone")] Models.Order order) // GET
        {
            // Autofill the date, user, and total properties instead of the user doing this
            order.OrderDate = DateTime.Now;
            order.UserId = User.Identity.Name;

            var cartItems = _context.Cart.Where(c => c.Username == User.Identity.Name);
            decimal cartTotal = (from c in cartItems select c.Quantity * c.Price).Sum();

            order.Total = cartTotal;


            // Will NEED and EXTENSION to the .Net Core Session object to store the order Object - in the next video!
            //HttpContext.Session.SetString("cartTotal", cartTotal.ToString());

            // We now have the session to the complex object

            HttpContext.Session.SetObject("Order", order);


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
            //Setup payment page to show order total

            // 1. Get the order from the session variable
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            // 2. Use Viewbag to display total and pass the amount to Stripe
            ViewBag.Total = order.Total;
            ViewBag.CentsTotal = order.Total * 100; // Stripe uses cents, not dollars and cents
            ViewBag.PublishableKey = _configuration.GetSection("Stripe")["PublishableKey"];




            return View();
        }

        // Need to get 2 things back from Stripe after authorization

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Payment(string stripeEmail, string stripeToken)
        {

            //send payment to stripe
            StripeConfiguration.ApiKey = _configuration.GetSection("Stripe")["SecretKey"];
            var cartUsername = HttpContext.Session.GetString("CartUsername");
            var cartItems = _context.Cart.Where(c => c.Username == cartUsername);
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            //new stripe payment attempt

            var customerService = new CustomerService();
            var chargeservices = new ChargeService();
            //new customer email from payment form, token auto-generated on payment form also
            var customer = customerService.Create(new CustomerCreateOptions
            {
                Email = stripeEmail,
                Source = stripeToken

            });

            //new charge using customer created above
            var charge = chargeservices.Create(new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(order.Total * 100),
                Description = "Georgian Computers Purchase",
                Currency = "cad",
                Customer = customer.Id
            });

            //generate and save new order
            _context.Order.Add(order);
            _context.SaveChanges();

            //save order details
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };
                _context.OrderDetail.Add(orderDetail);
            }
            _context.SaveChanges();


            //delete the cart
            foreach(var item in cartItems)
            {
                _context.Cart.Remove(item);
            }
            _context.SaveChanges();



            //confirm with a receipt for the new orderId








            return RedirectToAction("Details", "Orders", new { id = order.OrderId });



        }








    }



}
