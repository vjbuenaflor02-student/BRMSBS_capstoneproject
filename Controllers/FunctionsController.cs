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
            return View();
        }

        public IActionResult ManageRoomsA()
        {
            var rooms = _context.Rooms.ToList(); 
            return View(rooms);
        }

        public IActionResult CalendarIntergrationA()
        {
            return View();
        }

        public IActionResult ManageStaff()
        {
            var users = _context.User.ToList();
            return View(users);
        }

        public IActionResult SalesReports()
        {
            return View();
        }

        public IActionResult CancelBookReserve() 
        {
            var bookings = _context.Bookings.ToList();
            return View(bookings);
        }
    }
}
