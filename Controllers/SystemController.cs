using BRMSBS_capstoneproject.Data;
using BRMSBS_capstoneproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BRMSBS_capstoneproject.Controllers
{
    public class SystemController : Controller
    {
        private readonly MyAppContext _context;

        // Constructor to inject the DbContext
        public SystemController(MyAppContext context)
        {
            _context = context;
        }



        // GET: System/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: System/Logout - Logout action
        [HttpPost]
        public IActionResult Logout()
        {
            // Clear session or authentication here if used
            return RedirectToAction("Login", "System");
        }

        // Redirect to HomeDashboard after successful login for administrator
        public IActionResult HomeDashboardAdmin()
        {
            return View();
        }

        // Redirect to HomeDashboard after successful login for staff
        public IActionResult HomeDashboardStaff()
        {
            return View();
        }





        // ALL POSTS
        [HttpPost]
        // POST: System/Login
        public ActionResult Login(string username, string password)
        {
            // Admin credentials
            if (username == "adminuser" && password == "adminuser123")
            {
                return RedirectToAction("HomeDashboardAdmin", "System");
            }

            // Check user credentials from the database
            var user = _context.User.FirstOrDefault(u => u.Username == username);

            if (user != null && user.Password == ComputeSha256Hash(password))
            {
                return RedirectToAction("HomeDashboardStaff", "System");
            }

            return View();
        }

        // POST: System/CreateAccount
        public IActionResult CreateAccount(string Username, string Password)
        {
            // Hash password using SHA-256
            string hashedPassword = ComputeSha256Hash(Password); // Assuming you have this method

            // Create and save the user (implement your logic here)
            var user = new User { Username = Username, Password = hashedPassword };
            _context.User.Add(user);
            _context.SaveChanges(); // Save user to database

            TempData["AccountCreated"] = true;
            return RedirectToAction("ManageStaff", "Functions");
        }

        // POST: System/EditAccount
        public IActionResult EditAccount(int Id, string Username, string Password)
        {
            var user = _context.User.FirstOrDefault(u => u.Id == Id);
            if (user == null)
            {
                return NotFound();
            }

            user.Username = Username;

            if (!string.IsNullOrWhiteSpace(Password))
            {
                user.Password = ComputeSha256Hash(Password);
            }

            _context.SaveChanges();

            TempData["AccountEdited"] = "edited";
            return RedirectToAction("ManageStaff", "Functions");
        }

        // POST: System/DeleteAccount
        public IActionResult DeleteAccount(int Id)
        {
            var user = _context.User.FirstOrDefault(u => u.Id == Id);
            if (user == null)
            {
                return NotFound();
            }

            _context.User.Remove(user);
            _context.SaveChanges();

            TempData["AccountDeleted"] = true;
            return RedirectToAction("ManageStaff", "Functions");
        }

        // Hash a password using SHA-256
        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // POST: System/BookRoom
        [HttpPost]
        public IActionResult BookRoom([FromForm] BookingModel booking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                _context.SaveChanges();
                ModelState.Clear(); // Clear form fields after success
                TempData["BookingSuccess"] = true; // Set flag for success modal
                return RedirectToAction("BookingA", "Functions");
            }
            return View("BookingA", booking);
        }
    }
}
