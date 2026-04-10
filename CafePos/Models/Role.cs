using System.ComponentModel.DataAnnotations;

namespace CafePos.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; } 
        [Required]
        public string Name { get; set; } 

        // Navigation property
        public virtual ICollection<User> Users { get; set; }
    }
}
