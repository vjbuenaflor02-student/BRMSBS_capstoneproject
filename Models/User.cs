using System.ComponentModel.DataAnnotations;

namespace BRMSBS_capstoneproject.Models
{
    public class User
    {

        // Primary key
        [Key]
        public int Id { get; set; }

        // Username and Password fields
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
