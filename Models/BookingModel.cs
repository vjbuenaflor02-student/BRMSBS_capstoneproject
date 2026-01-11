using System.ComponentModel.DataAnnotations;
namespace BRMSBS_capstoneproject.Models;
public class BookingModel
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
    public string Status { get; set; } = "Checked In";
    public string BookReserve { get; set; } = "Booking";
    public string AccessBy { get; set; } = "";
    public string GuestNames { get; set; }

}