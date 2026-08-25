using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.DTOs
{
    // --- ATTENDANCE ---
    public class AttendanceCreateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid session must be specified.")]
        public int Session_ID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid student must be specified.")]
        public int Student_ID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid attendance status must be specified.")]
        public int Attendance_Status_ID { get; set; }
    }

    // --- FAQS (ADMIN CONTENT) ---
    public class FAQCreateDto
    {
        [Required]
        [StringLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Answer { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid FAQ category must be specified.")]
        public int FAQ_Category_ID { get; set; }

        [StringLength(1000)]
        public string? Applicable_Pages { get; set; }
    }

    public class FAQUpdateDto
    {
        [Required]
        [StringLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Answer { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid FAQ category must be specified.")]
        public int FAQ_Category_ID { get; set; }

        [StringLength(1000)]
        public string? Applicable_Pages { get; set; }
    }

    public class FAQCategoryCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Category_Name { get; set; } = string.Empty;
    }

    // --- MEDIA CONTENT ---
    public class MediaCreateDto
    {
        [Required]
        [StringLength(200)]
        public string Media_Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Media_Address { get; set; } = string.Empty;
    }

    // --- HELP PAGE ---
    public class HelpPageCreateDto
    {
        [Required]
        [StringLength(200)]
        public string Help_Page_Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Help_Page_Description { get; set; } = string.Empty;
    }

    public class HelpResourceCreateDto
    {
        [Required]
        [StringLength(200)]
        public string Video_Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Video_URL { get; set; } = string.Empty;
    }

    // --- TESTIMONIAL CATEGORY ---
    public class TestimonialCategoryCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Test_Category_Name { get; set; } = string.Empty;
    }

    public class TestimonialUpdateDto
    {
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid testimonial category must be specified.")]
        public int Testimonial_Category_ID { get; set; }
    }

    // --- INTERNAL REVIEWS ---
    public class TutorReviewCreateDto
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid student must be specified.")]
        public int Student_ID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid tutor must be specified.")]
        public int Tutor_ID { get; set; }
    }

    public class SessionReviewCreateDto
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid student must be specified.")]
        public int Student_ID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid session must be specified.")]
        public int Session_ID { get; set; }
    }

    // --- PUBLIC TESTIMONIALS ---
    public class TestimonialCreateDto
    {
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid student must be specified.")]
        public int Student_ID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid testimonial category must be specified.")]
        public int Testimonial_Category_ID { get; set; }
    }

    // --- BUSINESS RULES ---
    public class BusinessRuleUpdateDto
    {
        [Range(0, double.MaxValue)]
        public decimal Rule_Value { get; set; }
    }

    // --- ROLE NAV PERMISSIONS ---
    public class UpdateHiddenItemsDto
    {
        public List<string> HiddenItems { get; set; } = new();
    }

    // --- MODULE WISHLIST ---
    public class ModuleWishlistCreateDto
    {
        [Required(ErrorMessage = "Module code is required.")]
        [StringLength(20, ErrorMessage = "Module code cannot exceed 20 characters.")]
        public string Module_Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Module name is required.")]
        [StringLength(200, ErrorMessage = "Module name cannot exceed 200 characters.")]
        public string Module_Name { get; set; } = string.Empty;

        public int Student_ID { get; set; }
    }
}
