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
        public DbSet<User> User { get; set; }
    }
}
