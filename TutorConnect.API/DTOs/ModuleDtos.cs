using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.DTOs
{
    public class ModuleCreateDto
    {
        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string Module_Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Module_Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Module_Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater.")]
        public decimal Module_Price_OneOnOne { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater.")]
        public decimal Module_Price_Group { get; set; }
    }

    public class ModuleReturnDto
    {
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public string Module_Description { get; set; } = string.Empty;
        public decimal Module_Price_OneOnOne { get; set; }
        public decimal Module_Price_Group { get; set; }
    }

    public class TutorModuleAssignDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid tutor must be selected.")]
        public int Tutor_ID { get; set; }

        [Required(ErrorMessage = "Module code is required.")]
        public string Module_Code { get; set; } = string.Empty;
    }

    public class TutorModuleViewDto
    {
        public int Tutor_Module_ID { get; set; }
        public int Tutor_ID { get; set; }
        public string Tutor_Name { get; set; } = string.Empty;
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public DateTime Assigned_Date { get; set; }
        public bool IsActive { get; set; }
    }
}