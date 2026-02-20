using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BRMSBS_capstoneproject.Models
{
    public class ClientModel 
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
    }
}
