using BRMSBS_capstoneproject.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BRMSBS_capstoneproject.Controllers
{
    public class FunctionsController : Controller
    {
        private readonly MyAppContext _context;

        // Constructor to inject the DbContext
        public FunctionsController(MyAppContext context)
        {
            _context = context;
        }

        // ADMIMISTRATOR FUNCTIONS

        public IActionResult BookingA()
        {
            // Fetch only available rooms
            var rooms = _context.Rooms
                .Where(r => r.Status != "Occupied")
                .ToList();

            ViewBag.Rooms = rooms;
            return View();
        }

        public IActionResult ReservationA()
        {
            // Fetch available rooms for the reservation form
            var rooms = _context.Rooms
                .Where(r => r.Status == "Available")
                .ToList();

            ViewBag.Rooms = rooms;
            return View();
        }

        public IActionResult ManageRoomsA()
        {
            var rooms = _context.Rooms.ToList(); 
            return View(rooms);
        }

        public IActionResult ManageStaff()
        {
            var users = _context.User.ToList();
            return View(users);
        }

        public IActionResult SalesReports()
        {
            var customers = _context.Customers
                .Where(c => c.Status == "Purchased" || c.Status == "Cancelled")
                .ToList();
            return View(customers); // Pass the list to the view
        }

        public IActionResult CancelBooking() 
        {
            var bookings = _context.Bookings.ToList();
            return View(bookings);
        }

        public IActionResult CheckOut()
        {
            var bookings = _context.Bookings.ToList();
            return View(bookings);
        }

        public IActionResult CheckOutReserve()
        {
            var reserves = _context.Reservations.ToList();

            // Provide rooms list to the view so check-in modals can populate room numbers
            var rooms = _context.Rooms
                .Where(r => r.Status != "Occupied")
                .ToList();
            ViewBag.Rooms = rooms;

            return View(reserves);
        }

    }
}
