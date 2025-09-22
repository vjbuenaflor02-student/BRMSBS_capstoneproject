using System.ComponentModel.DataAnnotations;
namespace BRMSBS_capstoneproject.Models;

public class CustomerModel
{
    [Key]
    public int Id { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MI { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public int ContactNumber { get; set; }
    public string Nationality { get; set; }
    public string Purpose { get; set; }
    public DateTime ArrivalDate { get; set; }
    public DateTime DepartureDate { get; set; }
    public string RoomNumber { get; set; }
    public string RoomType { get; set; }
    public string RoomRates { get; set; }
    public int NumberOfPax { get; set; }
    public string Status { get; set; } = "Purchased";
    public string BookReserve { get; set; }
    public DateTime CheckOutDateTime { get; set; }
}
