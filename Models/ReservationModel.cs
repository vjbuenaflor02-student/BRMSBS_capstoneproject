    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
namespace BRMSBS_capstoneproject.Models
{
    public class ReservationModel
    {
        [Key]
        public int Id { get; set; }

        [Required]

        // Personal Information
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MI { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string Nationality { get; set; }
        public string Purpose { get; set; }

        // Booking/Reserve Information
        public DateTime ArrivalDate { get; set; }
        public DateTime DepartureDate { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public string RoomRates { get; set; }
        public int NumberOfPax { get; set; }
        public string Status { get; set; } = "Reserved";
        public string BookReserve { get; set; } = "Reservation";
        public string AccessBy { get; set; } = "";
        public string GuestNames { get; set; }


        // For Pay Later Original Reserve before reservation is made
        public double PayLaterOrigReserve { get; set; }

        // For Total Pay Reservation (Balance)
        public double TotalPayReserve { get; set; } 
    }
}
