using BRMSBS_capstoneproject.Models;
using Microsoft.EntityFrameworkCore;

namespace BRMSBS_capstoneproject.Data
{
    public class MyAppContext : DbContext
    {
        // Constructor to pass options to the base DbContext
        public MyAppContext(DbContextOptions<MyAppContext> options) : base(options)
        {
        }
        public DbSet<UserModel> User { get; set; }
        public DbSet<BookingModel> Bookings { get; set; }
        public DbSet<ReservationModel> Reservations { get; set; }
        public DbSet<RoomModel> Rooms { get; set; }
        public DbSet<PurchaseModel> Customers { get; set; }
    }
}
