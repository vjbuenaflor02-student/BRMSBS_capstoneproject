using BRMSBS_capstoneproject.Data;
using BRMSBS_capstoneproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
















        // ALL FUNCTIONS
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

            // If login fails, set error message
            ViewBag.LoginError = "*Incorrect username or password.";
            return View();
        }

        // POST: System/CreateAccount
        public IActionResult CreateAccount(string Username, string Password)
        {
            // Hash password using SHA-256
            string hashedPassword = ComputeSha256Hash(Password); // Assuming you have this method

            // Create and save the user (implement your logic here)
            var user = new UserModel { Username = Username, Password = hashedPassword };
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

        // POST: System/Logout - Logout action
        public IActionResult Logout()
        {
            // Clear session or authentication here if used
            return RedirectToAction("Login", "System");
        }


        // -- BOOKING --

        // POST: System/BookRoom
        public IActionResult BookRoom([FromForm] BookingModel booking)
        {

            if (ModelState.IsValid)
            {
                // Save booking
                _context.Bookings.Add(booking);

                // Update room status
                var room = _context.Rooms.FirstOrDefault(r => 
                r.RoomNumber == int.Parse(booking.RoomNumber) && 
                r.RoomType == booking.RoomType);
                if (room != null)
                {
                    room.Status = "Occupied";
                }

                _context.SaveChanges();

                ModelState.Clear(); // Clear form fields after success
                TempData["BookingSuccess"] = true; // Set flag for success modal
                return RedirectToAction("BookingA", "Functions");
            }
            return View("BookingA", booking);
        }

        public IActionResult BookRoomS([FromForm] BookingModel booking) // Staff Booking
        {

            if (ModelState.IsValid)
            {
                // Save booking
                _context.Bookings.Add(booking);

                // Update room status
                var room = _context.Rooms.FirstOrDefault(r =>
                r.RoomNumber == int.Parse(booking.RoomNumber) &&
                r.RoomType == booking.RoomType);
                if (room != null)
                {
                    room.Status = "Occupied";
                }

                _context.SaveChanges();

                ModelState.Clear(); // Clear form fields after success
                TempData["BookingSuccess"] = true; // Set flag for success modal
                return RedirectToAction("BookingS", "Functions");
            }
            return View("BookingS", booking);
        }

        // -- RESERVATION --


        // POST: System/BookRoom
        public IActionResult ReserveRoom([FromForm] BookingModel booking)
        {
            if (ModelState.IsValid)
            {
                // Save booking
                _context.Bookings.Add(booking);

                // Update room status
                var room = _context.Rooms.FirstOrDefault(r =>
                    r.RoomNumber == int.Parse(booking.RoomNumber) &&
                    r.RoomType == booking.RoomType);
                if (room != null)
                {
                    room.Status = "Occupied";
                    booking.BookReserve = "Reservation";
                    booking.Status = "Pending";
                }

                _context.SaveChanges();

                ModelState.Clear(); // Clear form fields after success
                TempData["ReservationSuccess"] = true; // Set flag for success modal
                return RedirectToAction("ReservationA", "Functions");
            }
            return View("ReservationA", booking);
        }

        public IActionResult ReserveRoomS([FromForm] BookingModel booking) // Staff Reservation
        {
            if (ModelState.IsValid)
            {
                // Save booking
                _context.Bookings.Add(booking);

                // Update room status
                var room = _context.Rooms.FirstOrDefault(r =>
                    r.RoomNumber == int.Parse(booking.RoomNumber) &&
                    r.RoomType == booking.RoomType);
                if (room != null)
                {
                    room.Status = "Occupied";
                    booking.BookReserve = "Reservation";
                    booking.Status = "Pending";
                }

                _context.SaveChanges();

                ModelState.Clear(); // Clear form fields after success
                TempData["ReservationSuccess"] = true; // Set flag for success modal
                return RedirectToAction("ReservationS", "Functions");
            }
            return View("ReservationS", booking);
        }

        // -- MANAGE ROOMS --

        // POST: SYSTEM/CreateRoomNRoomType
        public IActionResult CreateRoomNRoomType(int RoomNumber, string RoomType)
        {
            // Create and save the room (implement your logic here)
            var room = new RoomModel { RoomNumber = RoomNumber, RoomType = RoomType };
            _context.Rooms.Add(room);
            _context.SaveChanges(); // Save rooms to database

            TempData["RoomCreated"] = true;
            return RedirectToAction("ManageRoomsA", "Functions");
        }

        // POST: SYSTEM/EditRoomNRoomType
        public IActionResult EditRoomNRoomType(int Id, int RoomNumber, string RoomType)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.Id == Id);
            if (room == null)
            {
                return NotFound();
            }
            room.RoomNumber = RoomNumber;
            room.RoomType = RoomType;
            _context.SaveChanges();
            TempData["RoomEdited"] = "edited";
            return RedirectToAction("ManageRoomsA", "Functions");
        }

        // POST: SYSTEM/DeleteRoomNRoomType
        public IActionResult DeleteRoomNRoomType(int Id)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.Id == Id);
            if (room == null)
            {
                return NotFound();
            }
            _context.Rooms.Remove(room);
            _context.SaveChanges();
            TempData["RoomDeleted"] = true;
            return RedirectToAction("ManageRoomsA", "Functions");
        }
        public IActionResult SetRoomAsAvailable(int Id)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.Id == Id);
            if (room != null)
            {
                room.Status = "Available";
                _context.SaveChanges();
                TempData["RoomActivated"] = true; // Must match Razor key
            }
            return RedirectToAction("ManageRoomsA", "Functions");
        }

        // -- CANCEL BOOK-RESERVE --

        [HttpPost]
        public IActionResult CancelBooking(int id)
        {
            // Find booking
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking != null)
            {
                // Transfer data to CustomerModel
                var customer = new CustomerModel
                {
                    FirstName = booking.FirstName,
                    LastName = booking.LastName,
                    MI = booking.MI,
                    Address = booking.Address,
                    Email = booking.Email,
                    ContactNumber = int.TryParse(booking.ContactNumber, out var contactNum) ? contactNum : 0,
                    Nationality = booking.Nationality,
                    Purpose = booking.Purpose,
                    ArrivalDate = booking.ArrivalDate,
                    DepartureDate = booking.DepartureDate,
                    RoomNumber = booking.RoomNumber,
                    RoomType = booking.RoomType,
                    RoomRates = booking.RoomRates,
                    NumberOfPax = booking.NumberOfPax,
                    BookReserve = booking.BookReserve,
                    CheckOutDateTime = DateTime.Now, // Set checkout date/time
                    Payment = "None",
                    Status = "Cancelled"
                };

                // Save to database
                _context.Customers.Add(customer);


                // Find room and set to available
                var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber.ToString() == booking.RoomNumber && r.RoomType == booking.RoomType);
                if (room != null)
                {
                    room.Status = "Available";
                    _context.Rooms.Update(room);
                }

                // Clear booking data (or remove booking)
                _context.Bookings.Remove(booking);
                _context.SaveChanges();
            }
            TempData["CancelSuccess"] = true;
            TempData["CancelledBookingId"] = id;
            return RedirectToAction("CancelBookReserve", "Functions");
        }

        // -- CHECKOUT --

        [HttpPost]
        [Route("System/CheckOut/{bookingId}")]
        public IActionResult CheckOut(int bookingId, double grandTotal, string paymentOption)
        {
            // Find the booking by ID
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            // Transfer data to CustomerModel
            var customer = new CustomerModel
            {
                FirstName = booking.FirstName,
                LastName = booking.LastName,
                MI = booking.MI,
                Address = booking.Address,
                Email = booking.Email,
                ContactNumber = int.TryParse(booking.ContactNumber, out var contactNum) ? contactNum : 0,
                Nationality = booking.Nationality,
                Purpose = booking.Purpose,
                ArrivalDate = booking.ArrivalDate,
                DepartureDate = booking.DepartureDate,
                RoomNumber = booking.RoomNumber,
                RoomType = booking.RoomType,
                RoomRates = booking.RoomRates,
                NumberOfPax = booking.NumberOfPax,
                BookReserve = booking.BookReserve,
                CheckOutDateTime = DateTime.Now, // Set checkout date/time
                GrandAmount = grandTotal,  // Save grand total here
                Payment = paymentOption // Save payment option here
            };

            // Save to database
            _context.Customers.Add(customer);

            // Set room status to "Available"
            var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber.ToString() == booking.RoomNumber && r.RoomType == booking.RoomType);
            if (room != null)
            {
                room.Status = "Maintainance";
                _context.Rooms.Update(room);
            }

            // Remove the booking
            _context.Bookings.Remove(booking);
            _context.SaveChanges();

            // Redirect or show confirmation
            TempData["CheckOutSuccess"] = true;
            return RedirectToAction("CheckOut", "Functions");
        }

        // -- RESERVATION CHECK OUT --

        [HttpPost]
        public IActionResult ReservationCheckIn(int id)
        {
            // Find booking
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking != null)
            {
                booking.Status = "Checked In"; // Set status to "Checked In"
                _context.Bookings.Update(booking); // Mark entity as modified
                _context.SaveChanges();
            }
            TempData["CheckInSuccess"] = true;
            TempData["CheckInBookingId"] = id;
            return RedirectToAction("ReservationCheckInA", "Functions");
        }

        public IActionResult ReservationCheckInS(int id) // Staff Reservation Check In
        {
            // Find booking
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking != null)
            {
                booking.Status = "Checked In"; // Set status to "Checked In"
                _context.Bookings.Update(booking); // Mark entity as modified
                _context.SaveChanges();
            }
            TempData["CheckInSuccess"] = true;
            TempData["CheckInBookingId"] = id;
            return RedirectToAction("ReservationCheckInS", "Functions");
        }

        // - SALES REPORT --

        [HttpPost]
        public IActionResult DeleteCustomer(int customerId)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null)
            {   
                _context.Customers.Remove(customer);
                _context.SaveChanges();
                TempData["CustomerDeleted"] = true;
            }
            return RedirectToAction("SalesReports", "Functions");
        }
    }
}
