using System.ComponentModel.DataAnnotations;

namespace BRMSBS_capstoneproject.Models
{
    public class Customer
    {
        // Primary key
        [Key]
        public int Id { get; set; }

        // fields
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string MiddleInitial { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string EmailAddress { get; set; }
        [Required]
        public int ContactNumber { get; set; }
        [Required]
        public string Nationality { get; set; }
        [Required]
        public string Purpose { get; set; }
        [Required]
        public string SpecialReq { get; set; }



        [Required]
        public DateTime ArrivalDate { get; set; }
        [Required]
        public DateTime DepartDate { get; set; }
        


        public string RoomNumber { get; set; }
        [Required]
        public string RoomType { get; set; }
        [Required]
        public string RoomRates { get; set; }
        [Required]
        public int NumberofPax { get; set; }
        [Required]
        public string Status { get; set; }
    }
}


