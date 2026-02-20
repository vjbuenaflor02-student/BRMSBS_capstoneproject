using System.ComponentModel.DataAnnotations;

namespace BRMSBS_capstoneproject.Models
{
    public class RoomModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoomNumber { get; set; }
        public string RoomType { get; set; }
        public int RoomPrice { get; set; }
        public int RoomCapacity { get; set; }
        public string Status { get; set; } = "Available";
    }
}
