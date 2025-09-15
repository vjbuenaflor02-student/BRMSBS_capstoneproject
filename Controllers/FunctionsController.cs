using BRMSBS_capstoneproject.Data;
using Microsoft.AspNetCore.Mvc;

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
            return View();
        }

        public IActionResult ReservationA()
        {
            return View();
        }

        public IActionResult ManageRoomsA()
        {
            return View();
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
    }
}
