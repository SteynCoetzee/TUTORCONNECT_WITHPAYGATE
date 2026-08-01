using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.DTOs
{
    // --- ATTENDANCE ---
    public class AttendanceCreateDto
    {
        public int Session_ID { get; set; }
        public int Student_ID { get; set; }
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

        public int FAQ_Category_ID { get; set; }
    }

    public class FAQUpdateDto
    {
        [Required]
        [StringLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Answer { get; set; } = string.Empty;

        public int FAQ_Category_ID { get; set; }
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

        public int Testimonial_Category_ID { get; set; }
    }

    // --- INTERNAL REVIEWS ---
    public class TutorReviewCreateDto
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public int Student_ID { get; set; }
        public int Tutor_ID { get; set; }
    }

    public class SessionReviewCreateDto
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public int Student_ID { get; set; }
        public int Session_ID { get; set; }
    }

    // --- PUBLIC TESTIMONIALS ---
    public class TestimonialCreateDto
    {
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int Student_ID { get; set; }
        public int Testimonial_Category_ID { get; set; }
    }

    // --- BUSINESS RULES ---
    public class BusinessRuleUpdateDto
    {
        [Range(0, double.MaxValue)]
        public decimal Rule_Value { get; set; }
    }
}
