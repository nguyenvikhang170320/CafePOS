using System.ComponentModel.DataAnnotations;

namespace CafePos.Models
{
    public class Table
    {
        [Key]
        public int TableId { get; set; } // [cite: 276]

        [Required]
        public string TableName { get; set; } // [cite: 277]

        public bool IsActive { get; set; } = true;
    }
}
