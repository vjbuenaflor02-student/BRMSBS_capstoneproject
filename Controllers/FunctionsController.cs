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

        // Pages for Staff

        

        // Pages for Admin

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
            // Fetch only available rooms
            var rooms = _context.Rooms
                .Where(r => r.Status != "Pending")
                .ToList();
            ViewBag.Rooms = rooms;
            // Provide client list so the Reservation view can populate the client combobox
            var clients = _context.Clients.ToList();
            ViewBag.Clients = clients;
            return View();
        }

        public IActionResult ManageRoomsA()
        {
            var rooms = _context.Rooms.ToList(); 
            return View(rooms);
        }

        public IActionResult ClientListA()
        {
            var clients = _context.Clients.ToList();
            return View(clients);
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
            // Return reservation records to the CheckOutReserve view which expects ReservationModel
            var reserves = _context.Reservations.ToList();
            return View(reserves);
        }

    }
}
