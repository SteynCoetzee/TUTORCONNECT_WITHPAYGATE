using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.DTOs
{
    public class ResourceCreateDto
    {
        [Required]
        [StringLength(200)]
        public string Module_Resource_Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Module_Resource_Type_ID { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Module_Code { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Resource_URL { get; set; } = string.Empty;

        [StringLength(100)]
        public string Folder_Name { get; set; } = string.Empty;
    }

    public class ResourceVisibilityDto
    {
        public bool Is_Visible { get; set; }
    }

    public class FolderVisibilityDto
    {
        [Required]
        [StringLength(20)]
        public string Module_Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Folder_Name { get; set; } = string.Empty;

        public bool Is_Visible { get; set; }
    }

    public class LogHoursCreateDto
    {
        // Swagger will automatically format these nicely because of the DateOnly/TimeOnly types!
        public DateOnly Log_Hours_Date { get; set; }
        public TimeOnly Log_Hours_Time { get; set; }

        [Range(0.01, 24, ErrorMessage = "Hours must be between 0.01 and 24.")]
        public decimal Log_Hours_Amount { get; set; }

        public int Tutor_ID { get; set; }
    }
}