    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
namespace BRMSBS_capstoneproject.Models
{
    public class ReservationModel
    {
        [Key]
        public int Id { get; set; }

        [Required]

        // Personal Information - Register
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MI { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string Nationality { get; set; }
        public string Purpose { get; set; }

        // Booking/Reserve Information - Check-in/Check-out
        public DateTime ArrivalDate { get; set; }
        public DateTime DepartureDate { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public string RoomRates { get; set; }
        public int NumberOfPax { get; set; }
        public string Status { get; set; }
        public string BookReserve { get; set; } = "Reservation";
        public string AccessBy { get; set; } = "";
        public string GuestNames { get; set; }

        // Paid Reservation Check-in
        public double Total { get; set; } = 0;
        public double PaidReserve { get; set; } = 0;
        public double ChangeReserve { get; set; } = 0;

        // Reservation Balance/Extension
        public double ExtendBalance { get; set; } = 0;
    }
}
