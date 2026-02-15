using BRMSBS_capstoneproject.Data;
using BRMSBS_capstoneproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

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
            var rooms = _context.Rooms.ToList();
            return View(rooms);
        }

        public IActionResult CheckInSubMenu()
        {
            return View();
        }

        public IActionResult CheckOutSubMenu()
        {
            return View();
        }

        public IActionResult CancelSubMenu()
        {
            return View();
        }

        public IActionResult AdminOptionsMenu()
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
            // Ensure GuestNames from the form is assigned (in case model binding missed it)
            if (string.IsNullOrWhiteSpace(booking.GuestNames) && Request.Form.ContainsKey("GuestNames"))
            {
                booking.GuestNames = Request.Form["GuestNames"].ToString();
            }

                // Read paid booking amount from form (cash provided) - prefer explicit fields in the request
                double formPaid = 0.0;
                if (Request.Form.ContainsKey("PaidAmount"))
                {
                    double.TryParse(Request.Form["PaidAmount"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out formPaid);
                }
                else if (Request.Form.ContainsKey("CashPaidBooking"))
                {
                    double.TryParse(Request.Form["CashPaidBooking"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out formPaid);
                }
                else if (Request.Form.ContainsKey("cashamount"))
                {
                    double.TryParse(Request.Form["cashamount"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out formPaid);
                }

                // Compute total amount on server: room rate * number of nights and store in booking.Total
                double totalAmount = 0.0;
                try
                {
                    var nights = (booking.DepartureDate - booking.ArrivalDate).Days;
                    if (nights < 1) nights = 1;
                    if (!string.IsNullOrWhiteSpace(booking.RoomRates))
                    {
                        // RoomRates stored as plain number string (e.g. "499") in UI
                        if (double.TryParse(booking.RoomRates, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                        {
                            totalAmount = rate * nights;
                        }
                        else
                        {
                            // fallback: try to remove non-numeric chars
                            var cleaned = System.Text.RegularExpressions.Regex.Replace(booking.RoomRates, "[^0-9.\\-]", "");
                            double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate2);
                            totalAmount = rate2 * nights;
                        }
                    }
                }
                catch
                {
                    totalAmount = 0.0;
                }

                // Persist calculated total and payment details to the booking model
                booking.Total = Math.Round(totalAmount, 2);
                booking.PaidBooking = Math.Round(formPaid, 2);
                booking.ChangeBooking = Math.Round(Math.Max(0.0, formPaid - totalAmount), 2);
                // Ensure ExtendBalance is zero by default on new booking
                booking.ExtendBalance = 0.0;

                // Save booking
                _context.Bookings.Add(booking);

                // Update room status
                var room = _context.Rooms.FirstOrDefault(r =>
                r.RoomNumber == int.Parse(booking.RoomNumber) &&
                r.RoomType == booking.RoomType);
                if (room != null)
                {
                    booking.AccessBy = "Admin";
                    room.Status = "Occupied";
                }

            _context.SaveChanges();

            ModelState.Clear(); // Clear form fields after success
            TempData["BookingSuccess"] = true; // Set flag for success modal
            return RedirectToAction("BookingA", "Functions");
        }
            return View("BookingA", booking);
        }

        //public IActionResult BookRoomS([FromForm] BookingModel booking) // Staff Booking
        //{

        //    if (ModelState.IsValid)
        //    {
        //        // Save booking
        //        _context.Bookings.Add(booking);

        //        // Update room status
        //        var room = _context.Rooms.FirstOrDefault(r =>
        //        r.RoomNumber == int.Parse(booking.RoomNumber) &&
        //        r.RoomType == booking.RoomType);
        //        if (room != null)
        //        {
        //            room.Status = "Occupied";
        //        }

        //        _context.SaveChanges();

        //        ModelState.Clear(); // Clear form fields after success
        //        TempData["BookingSuccess"] = true; // Set flag for success modal
        //        return RedirectToAction("BookingS", "Functions");
        //    }
        //    return View("BookingS", booking);
        //}

        // -- RESERVATION --


        // POST: System/ReserveRoom
        public IActionResult ReserveRoom([FromForm] ReservationModel reserving)
        {
            if (ModelState.IsValid)
            {
                // Ensure GuestNames from the form is assigned (in case model binding missed it)
                if (string.IsNullOrWhiteSpace(reserving.GuestNames) && Request.Form.ContainsKey("GuestNames"))
                {
                    reserving.GuestNames = Request.Form["GuestNames"].ToString();
                }

                // Save reservation to Reservations set
                _context.Reservations.Add(reserving);

                // Update room status
                // Parse RoomNumber outside the EF expression to avoid expression tree compilation issues
                if (int.TryParse(reserving.RoomNumber, out var reservingRoomNum))
                {
                    var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber == reservingRoomNum && r.RoomType == reserving.RoomType);
                    if (room != null)
                    {
                        reserving.AccessBy = "Admin";
                        room.Status = "Reserved";
                    }
                }

                _context.SaveChanges();

                ModelState.Clear(); // Clear form fields after success
                TempData["ReservationSuccess"] = true; // Set flag for success modal
                return RedirectToAction("ReservationA", "Functions");
            }
            return View("ReservationA", reserving);
        }

        //public IActionResult ReserveRoomS([FromForm] BookingModel booking) // Staff Reservation
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Save booking
        //        _context.Bookings.Add(booking);

        //        // Update room status
        //        var room = _context.Rooms.FirstOrDefault(r =>
        //            r.RoomNumber == int.Parse(booking.RoomNumber) &&
        //            r.RoomType == booking.RoomType);
        //        if (room != null)
        //        {
        //            room.Status = "Occupied";
        //            booking.BookReserve = "Reservation";
        //            booking.Status = "Pending";
        //        }

        //        _context.SaveChanges();

        //        ModelState.Clear(); // Clear form fields after success
        //        TempData["ReservationSuccess"] = true; // Set flag for success modal
        //        return RedirectToAction("ReservationS", "Functions");
        //    }
        //    return View("ReservationS", booking);
        //}

        // -- MANAGE ROOMS --

        // POST: SYSTEM/CreateRoomNRoomType
        public IActionResult CreateRoomNRoomType(int RoomNumber, string RoomType, int RoomPrice, int RoomCapacity)
        {
            // Create and save the room (implement your logic here)
            var room = new RoomModel { RoomNumber = RoomNumber, RoomType = RoomType, RoomPrice = RoomPrice, RoomCapacity = RoomCapacity };
            _context.Rooms.Add(room);
            _context.SaveChanges(); // Save rooms to database

            TempData["RoomCreated"] = true;
            return RedirectToAction("ManageRoomsA", "Functions");
        }

        // POST: SYSTEM/EditRoomNRoomType
        public IActionResult EditRoomNRoomType(int Id, int RoomNumber, string RoomType, int RoomPrice, int RoomCapacity)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.Id == Id);
            if (room == null)
            {
                return NotFound();
            }
            room.RoomNumber = RoomNumber;
            room.RoomType = RoomType;
            room.RoomPrice = RoomPrice;
            room.RoomCapacity = RoomCapacity;
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
                var customer = new PurchaseModel
                {
                    // Customer Info
                    FirstName = booking.FirstName,
                    LastName = booking.LastName,
                    MI = booking.MI,
                    Address = booking.Address,
                    Email = booking.Email,
                    ContactNumber = int.TryParse(booking.ContactNumber, out var contactNum) ? contactNum : 0,
                    Nationality = booking.Nationality,
                    Purpose = booking.Purpose,

                    // Book/Reserve Info
                    ArrivalDate = booking.ArrivalDate,
                    DepartureDate = booking.DepartureDate,
                    RoomNumber = booking.RoomNumber,
                    RoomType = booking.RoomType,
                    RoomRates = booking.RoomRates,
                    NumberOfPax = booking.NumberOfPax,
                    BookReserve = booking.BookReserve,
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
            return RedirectToAction("CancelBooking", "Functions");
        }

        // -- CHECKOUT --

        [HttpPost]
        [Route("System/CheckOut/{bookingId}")]
        public IActionResult CheckOut(int bookingId, int stayingDays, double grandTotal)
        {
            // Find the booking by ID
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            // Parse posted payment values (form names used in the view)
            double postedCashAmount = 0.0;
            double postedCashChange = 0.0;
            try
            {
                if (Request.Form.ContainsKey("cashAmount"))
                    double.TryParse(Request.Form["cashAmount"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedCashAmount);
                else if (Request.Form.ContainsKey("cashamount"))
                    double.TryParse(Request.Form["cashamount"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedCashAmount);

                if (Request.Form.ContainsKey("cashChange"))
                    double.TryParse(Request.Form["cashChange"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedCashChange);
                else if (Request.Form.ContainsKey("cashchange"))
                    double.TryParse(Request.Form["cashchange"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedCashChange);
            }
            catch
            {
                // ignore parse errors and keep defaults
            }

            // Transfer data to PurchaseModel
            var customer = new PurchaseModel
            {
                // Customer Info
                FirstName = booking.FirstName,
                LastName = booking.LastName,
                MI = booking.MI,
                Address = booking.Address,
                Email = booking.Email,
                ContactNumber = int.TryParse(booking.ContactNumber, out var contactNum) ? contactNum : 0,
                Nationality = booking.Nationality,
                Purpose = booking.Purpose,

                // Book/Reserve Info
                ArrivalDate = booking.ArrivalDate,
                DepartureDate = booking.DepartureDate,
                StayingDays = stayingDays, // Save staying days
                RoomNumber = booking.RoomNumber,
                RoomType = booking.RoomType,
                RoomRates = booking.RoomRates,
                NumberOfPax = booking.NumberOfPax,
                BookReserve = booking.BookReserve,
                CheckOutDateTime = DateTime.Now,

                // Payment mapping:
                // - copy original booking payment details into purchase record
                Total = booking.Total,
                Paid = booking.PaidBooking,
                Change = booking.ChangeBooking,

                // - values coming from the payment modal when confirming checkout
                ExtendTotal = Math.Round(grandTotal, 2),
                ExtendPaid = Math.Round(postedCashAmount, 2),
                ExtendChange = Math.Round(postedCashChange, 2)
            };

            // Save to database
            _context.Customers.Add(customer);

            // Set room status to "Maintainance"
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
            // save formatted cash change to TempData so the view can display it (string avoids serialization error)
            TempData["CashChangeFormatted"] = customer.ExtendChange.ToString("C2", new System.Globalization.CultureInfo("en-PH"));
            return RedirectToAction("CheckOut", "Functions");
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

        // -- DAYS EXTENTION --
        [HttpPost]

        // FOR BOOKING EXTEND AND PAYMENT 
        public IActionResult ExtendBooking(int bookingId, DateTime newDepartureDate, int extendedNights)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            // Only allow extension to later date
            if (newDepartureDate <= booking.DepartureDate)
            {
                TempData["ExtendFailed"] = "New departure must be after original departure.";
                return RedirectToAction("CheckOut", "Functions");
            }

            // Update booking's departure date and optionally staying days related fields
            booking.DepartureDate = newDepartureDate;
            // If the booking model stores staying days or totals elsewhere, update as needed

            _context.Bookings.Update(booking);
            _context.SaveChanges();

            TempData["ExtendSuccess"] = true;
            return RedirectToAction("CheckOut", "Functions");
        }

        // POST: System/ExtendAndPay - handle extension payment and update booking departure and cash fields
        [HttpPost]
        public IActionResult ExtendAndPay(int bookingId, DateTime newDepartureDate, int extendedNights, string paymentOption, double? payAmount)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var err = new Dictionary<string, object> { ["success"] = false, ["error"] = "BookingNotFound" };
                    return Json(err);
                }
                return NotFound();
            }

            if (newDepartureDate <= booking.DepartureDate)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var err = new Dictionary<string, object> { ["success"] = false, ["error"] = "NewDepartureMustBeAfterOriginal" };
                    return Json(err);
                }
                TempData["ExtendFailed"] = "New departure must be after original departure.";
                return RedirectToAction("CheckOut", "Functions");
            }

            // compute extended price
            int roomRates = 0;
            int.TryParse(booking.RoomRates, out roomRates);
            var extendedPrice = roomRates * extendedNights;

            // normalize nullable pay amount
            var payAmt = payAmount ?? 0.0;

            // Determine payment amounts server-side to avoid trusting client-provided balances.
            // Normalize addedRemain from client if present but compute primary values from payAmt.
            double addedRemain = 0.0;
            if (Request.Form.ContainsKey("addedRemain"))
            {
                double.TryParse(Request.Form["addedRemain"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out addedRemain);
            }

            // Use server-stored existing extend balance (do not override from client values)
            double existingExtendBalance = booking.ExtendBalance;

            // Compute how much of the pay amount is applied to this extension (cap to extendedPrice)
            double paidForExtension = Math.Min(payAmt, Math.Max(0.0, extendedPrice));

            // If client provided a paidForExtension, try to parse it but do not allow it to exceed payAmt or extendedPrice
            if (Request.Form.ContainsKey("paidForExtension"))
            {
                double parsedPaid = 0.0;
                if (double.TryParse(Request.Form["paidForExtension"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsedPaid))
                {
                    // clamp parsedPaid to sensible bounds
                    parsedPaid = Math.Max(0.0, parsedPaid);
                    paidForExtension = Math.Min(parsedPaid, Math.Min(payAmt, extendedPrice));
                }
            }

            // compute extra (amount over extendedPrice) which can be treated as 'addedRemain' (credit)
            double extra = Math.Max(0.0, payAmt - extendedPrice);
            // prefer explicit addedRemain only if provided and positive
            if (addedRemain <= 0 && extra > 0) addedRemain = extra;

            // unpaid portion of this extension that should be added to outstanding balance
            var unpaidOfThisExtension = Math.Max(0.0, extendedPrice - paidForExtension);
            // new extend balance = prior outstanding + unpaid portion (do NOT add extra funds as additional debt)
            var newExtendBalance = Math.Round(Math.Max(0.0, existingExtendBalance + unpaidOfThisExtension), 2);
            booking.ExtendBalance = newExtendBalance;

            // update departure date
            booking.DepartureDate = newDepartureDate;

            // If the booking is currently checked in, mark it as extended for clarity
            try
            {
                if (!string.IsNullOrEmpty(booking.Status) && booking.Status.IndexOf("Checked In", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (booking.Status.IndexOf("Extend", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        booking.Status = "Checked In Extend";
                    }
                }
            }
            catch
            {
                // ignore any issues with status manipulation
            }

            _context.Bookings.Update(booking);
            _context.SaveChanges();

            TempData["ExtendSuccess"] = true;

            // Prepare values to return for AJAX clients
            //double newCashChange = booking.CashChange;
            var cashChangeDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // If this was an AJAX request, return JSON instead of redirecting so client-side code can handle UI updates
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var dict = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["bookingId"] = bookingId,
                    ["newDepartureDate"] = newDepartureDate.ToString("yyyy-MM-dd"),
                    ["extendedNights"] = extendedNights,
                    ["paid"] = Math.Round(paidForExtension, 2),
                    ["paidApplied"] = Math.Round(paidForExtension, 2),
                    ["extendedPrice"] = extendedPrice,
                    ["addedRemain"] = Math.Round(addedRemain, 2),
                    ["cashChangeDate"] = cashChangeDate
                };
                return Json(dict);
            }

            return RedirectToAction("CheckOut", "Functions");
        }
    }
}
