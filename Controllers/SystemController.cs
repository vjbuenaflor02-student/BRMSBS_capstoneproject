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

        // -- RESERVATION --

        // GET: show reservation page (redirect to the Functions/ReservationA view)
        [HttpGet]
        public IActionResult RegisterReserve()
        {
            return RedirectToAction("ReservationA", "Functions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterReserve(Models.ReservationModel reserv)
        {
            // Accept posted personal information and store a minimal reservation record.
            if (reserv == null)
            {
                reserv = new Models.ReservationModel();
            }

            var r = new Models.ReservationModel
            {
                FirstName = reserv.FirstName,
                LastName = reserv.LastName,
                MI = reserv.MI,
                Address = reserv.Address,
                Email = reserv.Email,
                ContactNumber = reserv.ContactNumber,
                Nationality = reserv.Nationality,
                Purpose = reserv.Purpose,

                // Leave booking-related fields empty/default as requested
                ArrivalDate = DateTime.MinValue,
                DepartureDate = DateTime.MinValue,
                RoomNumber = string.Empty,
                RoomType = string.Empty,
                RoomRates = string.Empty,
                NumberOfPax = 0,
                Status = "Registered",
                BookReserve = "Reservation",
                AccessBy = "Admin",
                GuestNames = string.Empty,

                // Payment defaults
                Total = 0.0,
                PaidReserve = 0.0,
                ChangeReserve = 0.0,
                ExtendBalance = 0.0
            };

            _context.Reservations.Add(r);
            _context.SaveChanges();

            TempData["ReservationSuccess"] = true;
            return RedirectToAction("ReservationA", "Functions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReserveRoom(Models.ReservationModel reserv)
        {
            if (reserv == null)
            {
                return RedirectToAction("CheckOutReserve", "Functions");
            }

            // Find existing reservation record
            var existing = _context.Reservations.FirstOrDefault(r => r.Id == reserv.Id);
            if (existing == null)
            {
                return NotFound();
            }

            // Update fields from posted form. Prefer server-side parsing when possible.
            try
            {
                existing.ArrivalDate = reserv.ArrivalDate;
                existing.DepartureDate = reserv.DepartureDate;
                existing.RoomType = reserv.RoomType;
                existing.RoomNumber = reserv.RoomNumber;
                existing.RoomRates = reserv.RoomRates;
                existing.NumberOfPax = reserv.NumberOfPax;
                existing.GuestNames = reserv.GuestNames;

                // Payment related - compute server-side to avoid trusting client-provided balance
                // Prefer posted PaidReserve (amount paid now) and posted Total. Compute remaining and
                // update Balance as (existing DB balance + unpaid portion of this payment).
                double postedPaid = 0.0;
                try
                {
                    // Prefer explicit form fields; accept several possible names used by the view JS
                    string[] paidKeys = new[] { "PaidReserve", "payAmount", "extendPayHiddenAmount", "extendPayAmount", "paidForExtension" };
                    foreach (var k in paidKeys)
                    {
                        if (Request.Form.ContainsKey(k))
                        {
                            double.TryParse(Request.Form[k].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedPaid);
                            break;
                        }
                    }
                }
                catch { postedPaid = 0.0; }

                double postedTotal = 0.0;
                try
                {
                    // Prefer posted Total form field if present; fallback to model-bound value
                    if (Request.Form.ContainsKey("Total"))
                    {
                        double.TryParse(Request.Form["Total"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedTotal);
                    }
                    else if (Request.Form.ContainsKey("hiddenTotal"))
                    {
                        double.TryParse(Request.Form["hiddenTotal"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedTotal);
                    }
                    else if (Request.Form.ContainsKey("grandTotal") || Request.Form.ContainsKey("grandTotalHidden"))
                    {
                        var key = Request.Form.ContainsKey("grandTotal") ? "grandTotal" : "grandTotalHidden";
                        double.TryParse(Request.Form[key].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedTotal);
                    }
                    else
                    {
                        postedTotal = reserv.Total;
                    }
                }
                catch { postedTotal = reserv.Total; }

                // existing DB balance
                double priorBalance = existing.ExtendBalance;

                // unpaid portion for this reservation = max(0, postedTotal - postedPaid)
                var unpaidOfThisPayment = Math.Max(0.0, postedTotal - postedPaid);

                // new balance is prior DB balance + unpaid portion
                existing.ExtendBalance = Math.Round(Math.Max(0.0, priorBalance + unpaidOfThisPayment), 2);

                // If unpaid computed zero but client supplied an explicit Balance field (hiddenBalance),
                // accept it as authoritative only if it's greater than priorBalance (helps when JS computed value
                // and posted it directly under name "Balance"). This is a safe fallback to reflect client-side
                // computation when server-side parsing failed to derive total/paid correctly.
                try
                {
                    if ((unpaidOfThisPayment == 0.0) && Request.Form.ContainsKey("ExtendBalance"))
                    {
                        double postedBal = 0.0;
                        if (double.TryParse(Request.Form["ExtendBalance"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out postedBal))
                        {
                            if (postedBal > priorBalance)
                            {
                                existing.ExtendBalance = Math.Round(postedBal, 2);
                            }
                        }
                    }
                }
                catch { }

                // accumulate paid and change
                existing.PaidReserve = Math.Round((existing.PaidReserve) + postedPaid, 2);
                existing.ChangeReserve = Math.Round(Math.Max(0.0, postedPaid - postedTotal), 2);

                // Save posted total for record (server-calculated if needed)
                existing.Total = Math.Round(postedTotal, 2);

                // Mark as reserved
                existing.Status = "Reserved";
                existing.BookReserve = "Reservation";
                existing.AccessBy = "Admin";

                // If room info is present, set corresponding RoomModel.Status = "Reserved"
                if (!string.IsNullOrWhiteSpace(existing.RoomNumber) && !string.IsNullOrWhiteSpace(existing.RoomType))
                {
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

                _context.Reservations.Update(existing);
                _context.SaveChanges();

                TempData["ReserveSuccess"] = true;
            }
            catch
            {
                TempData["ReserveFailed"] = true;
            }

            return RedirectToAction("CheckOutReserve", "Functions");
        }




















































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
    }
}
