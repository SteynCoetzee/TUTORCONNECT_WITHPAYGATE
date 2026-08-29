using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.DTOs
{
    // --- MODULE CRUD (ModulesController) ---

    // Request body for ModulesController.CreateModule/UpdateModule, and one row of the bulk-import sheet
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

    // Response shape for ModulesController.GetModules
    public class ModuleReturnDto
    {
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public string Module_Description { get; set; } = string.Empty;
        public decimal Module_Price_OneOnOne { get; set; }
        public decimal Module_Price_Group { get; set; }
    }

    // --- TUTOR-MODULE ASSIGNMENT (TutorModuleController) ---

    // Request body for TutorModuleController.AssignTutorToModule
    public class TutorModuleAssignDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid tutor must be selected.")]
        public int Tutor_ID { get; set; }

        [Required(ErrorMessage = "Module code is required.")]
        public string Module_Code { get; set; } = string.Empty;
    }

    // Response shape for TutorModuleController.GetModulesForTutor/GetTutorsForModule
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

    // --- BULK IMPORT (ModulesController — DownloadBulkTemplate / BulkCreateModules) ---

    // One validation failure on one row of the uploaded sheet
    public class ModuleBulkRowError
    {
        public int RowNumber { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // Response shape for BulkCreateModules — a summary of the whole import run
    public class ModuleBulkImportResult
    {
        public int TotalRowsProcessed { get; set; }
        public int SuccessCount { get; set; }
        public List<string> CreatedModuleCodes { get; set; } = new();
        public List<ModuleBulkRowError> Errors { get; set; } = new();
    }
}