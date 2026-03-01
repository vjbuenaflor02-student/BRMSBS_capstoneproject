using BRMSBS_capstoneproject.Data;
using BRMSBS_capstoneproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Text.Json;

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

        // ===============================================================================================================
        // ===============================================================================================================
        // ================================                  CORE FUNCTIONS                ===============================
        // ===============================================================================================================
        // ===============================================================================================================

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

        // GET: System/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: System/Login
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            // Admin credentials
            if (username == "adminuser" && password == "adminuser123")
            {
                // mark session as authenticated
                try { HttpContext.Session.SetString("IsAuthenticated", "true"); } catch { }
                return RedirectToAction("HomeDashboardAdmin", "System");
            }

            // Check user credentials from the database
            var user = _context.User.FirstOrDefault(u => u.Username == username);

            if (user != null && user.Password == ComputeSha256Hash(password))
            {
                try { HttpContext.Session.SetString("IsAuthenticated", "true"); } catch { }
                return RedirectToAction("HomeDashboardStaff", "System");
            }

            // If login fails, set error message
            ViewBag.LoginError = "*Incorrect username or password.";
            return View();
        }

        // POST: System/Logout - Logout action
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear any session flags
            try { HttpContext.Session.Clear(); } catch { }

            // Try to sign out any authentication schemes (no-op if none configured)
            try
            {
                await HttpContext.SignOutAsync();
            }
            catch
            {
                // ignore if sign-out is not configured
            }

            // Prevent caching of protected pages so the browser can't navigate back to them
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            // If this was called via AJAX (logout fetch), return success so client JS will navigate
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { success = true, redirect = Url.Action("Login", "System") });
            }

            return RedirectToAction("Login", "System");
        }

        // =================== Account management functions for staff accounts ================

        // POST: System/CreateAccount
        [HttpPost]
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

        
        // ===============================================================================================================
        // ===============================================================================================================
        // ================================               ADMINISTRATOR PAGES                =============================
        // ===============================================================================================================
        // ===============================================================================================================

        // Functions to redirect to admin pages after login
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

        // =================== Booking and reservation functions for administrator ================

        // ####### BOOKING - ADMIN ####### //

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

        // ####### RESERVATION - ADMIN ####### //

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReserveRoom(Models.ReservationModel reserv)
        {
            if (reserv == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Reservation data is required" });
                return RedirectToAction("CheckOutReserve", "Functions");
            }

            try
            {
                // Check if reservation exists or needs to be created
                ReservationModel existing = null;
                bool isNewReservation = false;

                if (reserv.Id <= 0)
                {
                    // Create new reservation with all required fields initialized
                    isNewReservation = true;
                    existing = new Models.ReservationModel
                    {
                        FirstName = reserv.FirstName?.Trim() ?? "",
                        LastName = reserv.LastName?.Trim() ?? "",
                        MI = reserv.MI?.Trim() ?? "",
                        Address = reserv.Address?.Trim() ?? "",
                        Email = reserv.Email?.Trim() ?? "",
                        ContactNumber = reserv.ContactNumber?.Trim() ?? "",
                        Nationality = reserv.Nationality?.Trim() ?? "",
                        Purpose = reserv.Purpose?.Trim() ?? "",
                        GuestNames = reserv.GuestNames?.Trim() ?? "",
                        RoomNumber = reserv.RoomNumber?.Trim() ?? "",
                        RoomType = reserv.RoomType?.Trim() ?? "",
                        RoomRates = reserv.RoomRates?.Trim() ?? "0",
                        NumberOfPax = reserv.NumberOfPax > 0 ? reserv.NumberOfPax : 1,
                        ArrivalDate = reserv.ArrivalDate != default ? reserv.ArrivalDate : DateTime.Now.AddDays(2),
                        DepartureDate = reserv.DepartureDate != default ? reserv.DepartureDate : DateTime.Now.AddDays(3),
                        BookReserve = "Reservation",
                        Status = "Pending",
                        AccessBy = "Admin",
                        Total = 0,
                        PaidReserve = 0,
                        ChangeReserve = 0,
                        ExtendBalance = 0
                    };
                }
                else
                {
                    // Find existing reservation
                    existing = _context.Reservations.FirstOrDefault(r => r.Id == reserv.Id);
                    if (existing == null)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return Json(new { success = false, message = "Reservation not found. Please try again." });
                        return NotFound();
                    }

                    // Update fields from posted form (for existing reservation)
                    if (!string.IsNullOrWhiteSpace(reserv.FirstName))
                        existing.FirstName = reserv.FirstName.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.LastName))
                        existing.LastName = reserv.LastName.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.MI))
                        existing.MI = reserv.MI.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.Address))
                        existing.Address = reserv.Address.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.Email))
                        existing.Email = reserv.Email.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.ContactNumber))
                        existing.ContactNumber = reserv.ContactNumber.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.Nationality))
                        existing.Nationality = reserv.Nationality.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.Purpose))
                        existing.Purpose = reserv.Purpose.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.GuestNames))
                        existing.GuestNames = reserv.GuestNames.Trim();

                    // Update room details if provided
                    if (!string.IsNullOrWhiteSpace(reserv.RoomType))
                        existing.RoomType = reserv.RoomType.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.RoomNumber))
                        existing.RoomNumber = reserv.RoomNumber.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.RoomRates))
                        existing.RoomRates = reserv.RoomRates.Trim();
                    if (reserv.NumberOfPax > 0)
                        existing.NumberOfPax = reserv.NumberOfPax;
                    if (reserv.ArrivalDate != default)
                        existing.ArrivalDate = reserv.ArrivalDate;
                    if (reserv.DepartureDate != default)
                        existing.DepartureDate = reserv.DepartureDate;
                }

                // ==================== PAYMENT PROCESSING ====================
                // Calculate total amount based on room rate and number of nights
                double totalAmount = 0.0;
                if (!string.IsNullOrWhiteSpace(existing.RoomRates) && existing.RoomRates != "0")
                {
                    if (double.TryParse(existing.RoomRates, out var roomRate) && existing.ArrivalDate != default && existing.DepartureDate != default)
                    {
                        var nights = Math.Ceiling((existing.DepartureDate - existing.ArrivalDate).TotalDays);
                        if (nights < 1) nights = 1;
                        totalAmount = roomRate * nights;
                    }
                }

                // Get the payment amount from the form (PaidReserve field or other possible names)
                double paymentAmount = 0.0;
                try
                {
                    string[] paidKeys = new[] { "PaidReserve", "payAmount", "PaymentAmount" };
                    foreach (var k in paidKeys)
                    {
                        if (Request.Form.ContainsKey(k))
                        {
                            double.TryParse(Request.Form[k].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out paymentAmount);
                            if (paymentAmount > 0) break;
                        }
                    }
                }
                catch { paymentAmount = 0.0; }

                // Only validate payment if we have room info and this is a final submission (with payment)
                if (!string.IsNullOrWhiteSpace(existing.RoomType) && !string.IsNullOrWhiteSpace(existing.RoomNumber) && paymentAmount > 0)
                {
                    if (paymentAmount < 750)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return Json(new { success = false, message = "Minimum payment is ₱750.00" });
                        return BadRequest("Minimum payment is ₱750.00");
                    }

                    // Calculate change and balance
                    double changeAmount = paymentAmount - totalAmount;
                    double balanceAmount = totalAmount - paymentAmount;

                    // Set the payment values
                    existing.Total = Math.Round(totalAmount, 2);
                    existing.PaidReserve = Math.Round(paymentAmount, 2);
                    existing.ChangeReserve = Math.Round(Math.Max(0, changeAmount), 2);
                    existing.ExtendBalance = Math.Round(Math.Max(0, balanceAmount), 2);

                    // Mark as reserved
                    existing.Status = "Reserved";

                    // Update room status
                    if (int.TryParse(existing.RoomNumber, out var roomNum))
                    {
                        var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber == roomNum && r.RoomType == existing.RoomType);
                        if (room != null)
                        {
                            room.Status = "Reserved";
                            _context.Rooms.Update(room);
                        }
                    }
                }

                // Update room status if room number is specified
                if (int.TryParse(existing.RoomNumber, out var roomNumForUpdate) && !string.IsNullOrWhiteSpace(existing.RoomType))
                {
                    var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber == roomNumForUpdate && r.RoomType == existing.RoomType);
                    if (room != null)
                    {
                        // Mark room as Reserved when reservation is created or updated
                        room.Status = "Reserved";
                        _context.Rooms.Update(room);
                    }
                }

                // Save or add to context
                if (isNewReservation)
                {
                    _context.Reservations.Add(existing);
                }
                else
                {
                    _context.Reservations.Update(existing);
                }

                _context.SaveChanges();

                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        message = "Reservation saved successfully",
                        reservationId = existing.Id,
                        total = existing.Total,
                        paidAmount = existing.PaidReserve,
                        changeAmount = existing.ChangeReserve,
                        balance = existing.ExtendBalance
                    });
                }

                TempData["ReserveSuccess"] = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in ReserveRoom: " + ex.ToString());
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Failed to save reservation: " + ex.Message });
                }
                TempData["ReserveFailed"] = true;
            }

            return RedirectToAction("CheckOutReserve", "Functions");
        }

        [HttpGet]
        public IActionResult GetLatestReservationId()
        {
            try
            {
                // Get the latest reservation ID by checking the most recently created record
                var latestReservation = _context.Reservations
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault();

                if (latestReservation != null)
                {
                    return Json(new { id = latestReservation.Id });
                }

                return BadRequest(new { error = "No reservations found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        // ####### CHECKOUT FUNCTIONS - ADMIN ####### //

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

        // POST: System/CheckOutReserve - Checkout for reservations
        [HttpPost]
        [Route("System/CheckOutReserve/{reservationId}")]
        public IActionResult CheckOutReserve(int reservationId, int stayingDays, double grandTotal)
        {
            // Find the reservation by ID
            var reservation = _context.Reservations.FirstOrDefault(r => r.Id == reservationId);
            if (reservation == null)
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
                FirstName = reservation.FirstName,
                LastName = reservation.LastName,
                MI = reservation.MI,
                Address = reservation.Address,
                Email = reservation.Email,
                ContactNumber = int.TryParse(reservation.ContactNumber, out var contactNum) ? contactNum : 0,
                Nationality = reservation.Nationality,
                Purpose = reservation.Purpose,

                // Book/Reserve Info
                ArrivalDate = reservation.ArrivalDate,
                DepartureDate = reservation.DepartureDate,
                StayingDays = stayingDays, // Save staying days
                RoomNumber = reservation.RoomNumber,
                RoomType = reservation.RoomType,
                RoomRates = reservation.RoomRates,
                NumberOfPax = reservation.NumberOfPax,
                BookReserve = reservation.BookReserve,
                CheckOutDateTime = DateTime.Now,

                // Payment mapping:
                // - copy original reservation payment details into purchase record
                Total = reservation.Total,
                Paid = reservation.PaidReserve,
                Change = reservation.ChangeReserve,

                // - values coming from the payment modal when confirming checkout
                ExtendTotal = Math.Round(grandTotal, 2),
                ExtendPaid = Math.Round(postedCashAmount, 2),
                ExtendChange = Math.Round(postedCashChange, 2)
            };

            // Save to database
            _context.Customers.Add(customer);

            // Set room status to "Maintainance"
            if (!string.IsNullOrWhiteSpace(reservation.RoomNumber) && !string.IsNullOrWhiteSpace(reservation.RoomType))
            {
                if (int.TryParse(reservation.RoomNumber, out var roomNum))
                {
                    var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber == roomNum && r.RoomType == reservation.RoomType);
                    if (room != null)
                    {
                        room.Status = "Maintainance";
                        _context.Rooms.Update(room);
                    }
                }
            }

            // Remove the reservation
            _context.Reservations.Remove(reservation);
            _context.SaveChanges();

            // Redirect or show confirmation
            TempData["CheckOutSuccess"] = true;
            // save formatted cash change to TempData so the view can display it (string avoids serialization error)
            TempData["CashChangeFormatted"] = customer.ExtendChange.ToString("C2", new System.Globalization.CultureInfo("en-PH"));
            return RedirectToAction("CheckOutReserve", "Functions");
        }


        // ####### DAYS EXTENTION - ADMIN ####### //
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

        // FOR RESERVATION EXTEND AND PAYMENT (handles extend stay payment for reservations)
        [HttpPost]
        public IActionResult ExtendStay(int bookingId, DateTime newCheckoutDate, int extendedNights, double extendedPrice, double payAmount, double extendBalance, double changeAmount)
        {
            var reservation = _context.Reservations.FirstOrDefault(r => r.Id == bookingId);
            if (reservation == null)
            {
                return NotFound();
            }

            // Validate that new checkout date is after current departure date
            if (newCheckoutDate <= reservation.DepartureDate)
            {
                TempData["ExtendFailed"] = "New departure must be after original departure.";
                return RedirectToAction("CheckOutReserve", "Functions");
            }

            // Update the reservation's departure date
            reservation.DepartureDate = newCheckoutDate;

            // Add the amount balance (unpaid portion) to the existing ExtendBalance
            // extendBalance parameter contains the remaining amount owed from this payment
            reservation.ExtendBalance = Math.Round(reservation.ExtendBalance + extendBalance, 2);

            // Update reservation in database
            _context.Reservations.Update(reservation);
            _context.SaveChanges();

            TempData["ExtendSuccess"] = true;
            TempData["ExtendedNights"] = extendedNights.ToString();
            TempData["ExtendedPrice"] = extendedPrice.ToString("C2");
            TempData["PaidAmount"] = payAmount.ToString("C2");
            TempData["ChangeAmount"] = changeAmount.ToString("C2");
            TempData["UpdatedExtendBalance"] = reservation.ExtendBalance.ToString("C2");
            TempData["UpdatedCheckOutDate"] = newCheckoutDate.ToString("M/d/yy");

            return RedirectToAction("CheckOutReserve", "Functions");
        }

        // POST: System/CheckInReservation - Check-in with payment for reservations
        [HttpPost]
        public IActionResult CheckInReservation(Models.ReservationModel reserv)
        {
            if (reserv == null || reserv.Id <= 0)
            {
                TempData["CheckInFailed"] = "Invalid reservation data.";
                return RedirectToAction("CheckOutReserve", "Functions");
            }

            try
            {
                // Find the reservation by ID
                var existing = _context.Reservations.FirstOrDefault(r => r.Id == reserv.Id);
                if (existing == null)
                {
                    TempData["CheckInFailed"] = "Reservation not found.";
                    return RedirectToAction("CheckOutReserve", "Functions");
                }

                // Update room details if provided
                if (!string.IsNullOrWhiteSpace(reserv.RoomType))
                    existing.RoomType = reserv.RoomType.Trim();
                if (!string.IsNullOrWhiteSpace(reserv.RoomNumber))
                    existing.RoomNumber = reserv.RoomNumber.Trim();
                if (!string.IsNullOrWhiteSpace(reserv.RoomRates))
                    existing.RoomRates = reserv.RoomRates.Trim();
                if (reserv.NumberOfPax > 0)
                    existing.NumberOfPax = reserv.NumberOfPax;
                if (reserv.ArrivalDate != default)
                    existing.ArrivalDate = reserv.ArrivalDate;
                if (reserv.DepartureDate != default)
                    existing.DepartureDate = reserv.DepartureDate;
                if (!string.IsNullOrWhiteSpace(reserv.GuestNames))
                    existing.GuestNames = reserv.GuestNames.Trim();

                // Get the payment amount from the form - this is what the user paid
                double paymentAmount = 0.0;
                try
                {
                    string[] paidKeys = new[] { "PaidReserve", "payAmount", "PaymentAmount" };
                    foreach (var k in paidKeys)
                    {
                        if (Request.Form.ContainsKey(k))
                        {
                            double.TryParse(Request.Form[k].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out paymentAmount);
                            if (paymentAmount > 0) break;
                        }
                    }
                }
                catch { paymentAmount = 0.0; }

                // Validate payment - must be at least 750 or user paid nothing (balance was zero)
                double currentExtendBalance = Math.Round(existing.ExtendBalance, 2);
                double displayCashChange = 0.0;

                if (paymentAmount > 0)
                {
                    // Only validate minimum if there's actually an ExtendBalance to pay
                    if (currentExtendBalance > 0 && paymentAmount < 750)
                    {
                        TempData["CheckInFailed"] = "Minimum payment is ₱750.00";
                        return RedirectToAction("CheckOutReserve", "Functions");
                    }

                    // Calculate new ExtendBalance: deduct payment from current ExtendBalance
                    // If payment exceeds ExtendBalance, ExtendBalance becomes 0
                    double newExtendBalance = Math.Max(0, Math.Round(currentExtendBalance - paymentAmount, 2));

                    // Calculate cash change: any payment amount exceeding the current ExtendBalance
                    displayCashChange = Math.Max(0, Math.Round(paymentAmount - currentExtendBalance, 2));

                    // ONLY modify ExtendBalance in the database
                    existing.ExtendBalance = newExtendBalance;
                }
                else if (currentExtendBalance == 0)
                {
                    // No payment needed, balance was already zero
                    existing.ExtendBalance = 0;
                    displayCashChange = 0;
                }

                // Mark status as "Checked-In"
                existing.Status = "Checked-In";

                // Update room status to Occupied
                if (int.TryParse(existing.RoomNumber, out var roomNum))
                {
                    var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber == roomNum && r.RoomType == existing.RoomType);
                    if (room != null)
                    {
                        room.Status = "Occupied";
                        _context.Rooms.Update(room);
                    }
                }

                // Update the reservation in database
                _context.Reservations.Update(existing);
                _context.SaveChanges();

                // Set success flag - store only string values in TempData (no raw doubles)
                TempData["CheckInSuccess"] = true;
                TempData["CheckedInBookingId"] = existing.Id;
                TempData["CashChangeFormatted"] = displayCashChange.ToString("C0", new System.Globalization.CultureInfo("en-PH"));
                TempData["ExtendBalanceFormatted"] = existing.ExtendBalance.ToString("C0", new System.Globalization.CultureInfo("en-PH"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in CheckInReservation: " + ex.ToString());
                TempData["CheckInFailed"] = "An error occurred during check-in. Please try again.";
            }

            return RedirectToAction("CheckOutReserve", "Functions");
        }

        // POST: System/QuickCheckIn - Quick check-in for Reserved reservations
        [HttpPost]
        public IActionResult QuickCheckIn(int bookingId)
        {
            try
            {
                // Find the reservation by ID
                var reservation = _context.Reservations.FirstOrDefault(r => r.Id == bookingId);
                if (reservation == null)
                {
                    TempData["QuickCheckInFailed"] = "Reservation not found.";
                    return RedirectToAction("CheckOutReserve", "Functions");
                }

                // Check if the reservation status is "Reserved"
                if (!string.Equals(reservation.Status, "Reserved", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["QuickCheckInFailed"] = "Only reservations with 'Reserved' status can be checked in.";
                    return RedirectToAction("CheckOutReserve", "Functions");
                }

                // Update status to "Checked-In"
                reservation.Status = "Checked-In";

                // Update the reservation in database
                _context.Reservations.Update(reservation);
                _context.SaveChanges();

                // Set success flag
                TempData["QuickCheckInSuccess"] = true;
                TempData["CheckedInBookingId"] = bookingId;

                return RedirectToAction("CheckOutReserve", "Functions");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in QuickCheckIn: " + ex.ToString());
                TempData["QuickCheckInFailed"] = "An error occurred during check-in. Please try again.";
                return RedirectToAction("CheckOutReserve", "Functions");
            }
        }

        // ####### LOGS HISTORY - ADMIN ####### //

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

        // ####### MANAGE ROOMS ####### //

        // POST: SYSTEM/CreateRoomNRoomType
        public IActionResult CreateRoomNRoomType(int RoomNumber, string RoomType, int RoomPrice, int RoomCapacity)
        {
            // Create and save the room (implement your logic here)
            var room = new RoomModel { RoomNumber = RoomNumber, RoomType = RoomType, RoomPrice = RoomPrice, RoomCapacity = RoomCapacity };
            _context.Rooms.Add(room);
            _context.SaveChanges(); // Save rooms to database

            TempData["RoomCreated"] = true;
            return RedirectToAction("ManageRoomA", "Functions");
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
            return RedirectToAction("ManageRoomA", "Functions");
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
            return RedirectToAction("ManageRoomA", "Functions");
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
            return RedirectToAction("ManageRoomA", "Functions");
        }

        // ===============================================================================================================
        // ===============================================================================================================
        // ================================                   STAFF PAGES                =================================
        // ===============================================================================================================
        // ===============================================================================================================


        // Redirect to HomeDashboard after successful login for staff
        public IActionResult HomeDashboardStaff()
        {
            var rooms = _context.Rooms.ToList();
            return View(rooms);
        }

        // POST: System/BookRoom
        public IActionResult BookRoomStaff([FromForm] BookingModel booking)
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
                    booking.AccessBy = "Staff";
                    room.Status = "Occupied";
                }

                _context.SaveChanges();

                ModelState.Clear(); // Clear form fields after success
                TempData["BookingSuccess"] = true; // Set flag for success modal
                return RedirectToAction("BookingA", "Functions");
            }
            return View("BookingA", booking);
        }

        // ####### RESERVATION - ADMIN ####### //

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReserveRoomStaff(Models.ReservationModel reserv)
        {
            if (reserv == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Reservation data is required" });
                return RedirectToAction("CheckOutReserve", "Functions");
            }

            try
            {
                // Check if reservation exists or needs to be created
                ReservationModel existing = null;
                bool isNewReservation = false;

                if (reserv.Id <= 0)
                {
                    // Create new reservation with all required fields initialized
                    isNewReservation = true;
                    existing = new Models.ReservationModel
                    {
                        FirstName = reserv.FirstName?.Trim() ?? "",
                        LastName = reserv.LastName?.Trim() ?? "",
                        MI = reserv.MI?.Trim() ?? "",
                        Address = reserv.Address?.Trim() ?? "",
                        Email = reserv.Email?.Trim() ?? "",
                        ContactNumber = reserv.ContactNumber?.Trim() ?? "",
                        Nationality = reserv.Nationality?.Trim() ?? "",
                        Purpose = reserv.Purpose?.Trim() ?? "",
                        GuestNames = reserv.GuestNames?.Trim() ?? "",
                        RoomNumber = reserv.RoomNumber?.Trim() ?? "",
                        RoomType = reserv.RoomType?.Trim() ?? "",
                        RoomRates = reserv.RoomRates?.Trim() ?? "0",
                        NumberOfPax = reserv.NumberOfPax > 0 ? reserv.NumberOfPax : 1,
                        ArrivalDate = reserv.ArrivalDate != default ? reserv.ArrivalDate : DateTime.Now.AddDays(2),
                        DepartureDate = reserv.DepartureDate != default ? reserv.DepartureDate : DateTime.Now.AddDays(3),
                        BookReserve = "Reservation",
                        AccessBy = "Staff",
                        Status = "Pending",
                        Total = 0,
                        PaidReserve = 0,
                        ChangeReserve = 0,
                        ExtendBalance = 0
                    };
                }
                else
                {
                    // Find existing reservation
                    existing = _context.Reservations.FirstOrDefault(r => r.Id == reserv.Id);
                    if (existing == null)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return Json(new { success = false, message = "Reservation not found. Please try again." });
                        return NotFound();
                    }

                    // Update fields from posted form (for existing reservation)
                    if (!string.IsNullOrWhiteSpace(reserv.FirstName))
                        existing.FirstName = reserv.FirstName.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.LastName))
                        existing.LastName = reserv.LastName.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.MI))
                        existing.MI = reserv.MI.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.Address))
                        existing.Address = reserv.Address.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.Email))
                        existing.Email = reserv.Email.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.ContactNumber))
                        existing.ContactNumber = reserv.ContactNumber.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.Nationality))
                        existing.Nationality = reserv.Nationality.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.Purpose))
                        existing.Purpose = reserv.Purpose.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.GuestNames))
                        existing.GuestNames = reserv.GuestNames.Trim();

                    // Update room details if provided
                    if (!string.IsNullOrWhiteSpace(reserv.RoomType))
                        existing.RoomType = reserv.RoomType.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.RoomNumber))
                        existing.RoomNumber = reserv.RoomNumber.Trim();
                    if (!string.IsNullOrWhiteSpace(reserv.RoomRates))
                        existing.RoomRates = reserv.RoomRates.Trim();
                    if (reserv.NumberOfPax > 0)
                        existing.NumberOfPax = reserv.NumberOfPax;
                    if (reserv.ArrivalDate != default)
                        existing.ArrivalDate = reserv.ArrivalDate;
                    if (reserv.DepartureDate != default)
                        existing.DepartureDate = reserv.DepartureDate;
                }

                // ==================== PAYMENT PROCESSING ====================
                // Calculate total amount based on room rate and number of nights
                double totalAmount = 0.0;
                if (!string.IsNullOrWhiteSpace(existing.RoomRates) && existing.RoomRates != "0")
                {
                    if (double.TryParse(existing.RoomRates, out var roomRate) && existing.ArrivalDate != default && existing.DepartureDate != default)
                    {
                        var nights = Math.Ceiling((existing.DepartureDate - existing.ArrivalDate).TotalDays);
                        if (nights < 1) nights = 1;
                        totalAmount = roomRate * nights;
                    }
                }

                // Get the payment amount from the form (PaidReserve field or other possible names)
                double paymentAmount = 0.0;
                try
                {
                    string[] paidKeys = new[] { "PaidReserve", "payAmount", "PaymentAmount" };
                    foreach (var k in paidKeys)
                    {
                        if (Request.Form.ContainsKey(k))
                        {
                            double.TryParse(Request.Form[k].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out paymentAmount);
                            if (paymentAmount > 0) break;
                        }
                    }
                }
                catch { paymentAmount = 0.0; }

                // Only validate payment if we have room info and this is a final submission (with payment)
                if (!string.IsNullOrWhiteSpace(existing.RoomType) && !string.IsNullOrWhiteSpace(existing.RoomNumber) && paymentAmount > 0)
                {
                    if (paymentAmount < 750)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return Json(new { success = false, message = "Minimum payment is ₱750.00" });
                        return BadRequest("Minimum payment is ₱750.00");
                    }

                    // Calculate change and balance
                    double changeAmount = paymentAmount - totalAmount;
                    double balanceAmount = totalAmount - paymentAmount;

                    // Set the payment values
                    existing.Total = Math.Round(totalAmount, 2);
                    existing.PaidReserve = Math.Round(paymentAmount, 2);
                    existing.ChangeReserve = Math.Round(Math.Max(0, changeAmount), 2);
                    existing.ExtendBalance = Math.Round(Math.Max(0, balanceAmount), 2);

                    // Mark as reserved
                    existing.Status = "Reserved";

                    // Update room status
                    if (int.TryParse(existing.RoomNumber, out var roomNum))
                    {
                        var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber == roomNum && r.RoomType == existing.RoomType);
                        if (room != null)
                        {
                            room.Status = "Reserved";
                            _context.Rooms.Update(room);
                        }
                    }
                }

                // Update room status if room number is specified
                if (int.TryParse(existing.RoomNumber, out var roomNumForUpdate) && !string.IsNullOrWhiteSpace(existing.RoomType))
                {
                    var room = _context.Rooms.FirstOrDefault(r => r.RoomNumber == roomNumForUpdate && r.RoomType == existing.RoomType);
                    if (room != null)
                    {
                        // Mark room as Reserved when reservation is created or updated
                        room.Status = "Reserved";
                        _context.Rooms.Update(room);
                    }
                }

                // Save or add to context
                if (isNewReservation)
                {
                    _context.Reservations.Add(existing);
                }
                else
                {
                    _context.Reservations.Update(existing);
                }

                _context.SaveChanges();

                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        message = "Reservation saved successfully",
                        reservationId = existing.Id,
                        total = existing.Total,
                        paidAmount = existing.PaidReserve,
                        changeAmount = existing.ChangeReserve,
                        balance = existing.ExtendBalance
                    });
                }

                TempData["ReserveSuccess"] = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in ReserveRoom: " + ex.ToString());
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Failed to save reservation: " + ex.Message });
                }
                TempData["ReserveFailed"] = true;
            }

            return RedirectToAction("CheckOutReserve", "Functions");
        }

        [HttpGet]
        public IActionResult GetLatestReservationIdStaff()
        {
            try
            {
                // Get the latest reservation ID by checking the most recently created record
                var latestReservation = _context.Reservations
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault();

                if (latestReservation != null)
                {
                    return Json(new { id = latestReservation.Id });
                }

                return BadRequest(new { error = "No reservations found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
