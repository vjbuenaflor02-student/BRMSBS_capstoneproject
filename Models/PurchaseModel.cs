using System.ComponentModel.DataAnnotations;
namespace BRMSBS_capstoneproject.Models;

public class PurchaseModel
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
    public int ContactNumber { get; set; }
    public string Nationality { get; set; }
    public string Purpose { get; set; }

    // Booking/Reserve Information
    public DateTime ArrivalDate { get; set; }
    public DateTime DepartureDate { get; set; }
    public int StayingDays { get; set; }
    public string RoomNumber { get; set; }
    public string RoomType { get; set; }
    public string RoomRates { get; set; }
    public int NumberOfPax { get; set; }
    public string Status { get; set; } = "Purchased";
    public string BookReserve { get; set; }
    public DateTime CheckOutDateTime { get; set; }

    // Payment 
    public double Total { get; set; } = 0;
    public double Paid { get; set; } = 0; // Cash on hand
    public double Change { get; set; } = 0;
    public double ExtendTotal { get; set; } = 0;
    public double ExtendPaid { get; set; } = 0; // Cash on hand
    public double ExtendChange { get; set; } = 0; 
}
