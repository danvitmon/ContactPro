using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ContactPro.Models
{
    public class Contact
    {
        // Primary Key
        public int Id { get; set; }

        // Foreign Key
        [Required]
        public string? AppUserId { get; set; }


        [Required]
        [Display(Name = "Objective Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
        public string? FirstName { get; set; }

        public string? Complete { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Date Created")]
        [DataType(DataType.Date)]
        public DateTime DateCreated { get; set; }


        // Image Properties
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public byte[]? ImageData { get; set; }

        public string? ImageType { get; set; }


        // Navigation Properties
        public virtual AppUser? AppUser { get; set; }

        public virtual ICollection<Category> Categories { get; set; } = new HashSet<Category>();
    }
}
