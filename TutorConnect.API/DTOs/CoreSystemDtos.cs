using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.DTOs
{
    // ─── ENROLLMENT (EnrollmentController) ────────────────────────

    // Request body for EnrollmentController.EnrollInModule
    public class EnrollmentCreateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid student must be selected.")]
        public int Student_ID { get; set; }

        [Required(ErrorMessage = "Module code is required.")]
        public string Module_Code { get; set; } = string.Empty;

        public bool Can_Book_OneOnOne { get; set; } = true;
        public bool Can_Book_Group { get; set; } = true;
    }

    // Request body for EnrollmentController.UnenrollFromModule
    public class EnrollmentUnenrollDto
    {
        [StringLength(1000, ErrorMessage = "Unenrol reason cannot exceed 1000 characters.")]
        public string? Unenroll_Reason { get; set; }
    }

    // Response shape for EnrollmentController.GetStudentModules/GetModuleStudents
    public class EnrollmentViewDto
    {
        public int Enrollment_ID { get; set; }
        public int Student_ID { get; set; }
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public DateTime Enrollment_Date { get; set; }
        public bool IsActive { get; set; }
        public bool Can_Book_OneOnOne { get; set; }
        public bool Can_Book_Group { get; set; }
        public int Sessions_Remaining_Group { get; set; }
        public int Sessions_Remaining_OneOnOne { get; set; }
    }

    // Request body for EnrollmentController.TopUpSessions
    public class SessionTopUpDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid student must be specified.")]
        public int Student_ID { get; set; }

        [Required(ErrorMessage = "Module code is required.")]
        [StringLength(20, ErrorMessage = "Module code cannot exceed 20 characters.")]
        public string Module_Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Session type is required.")]
        [StringLength(50, ErrorMessage = "Session type cannot exceed 50 characters.")]
        public string Session_Type { get; set; } = string.Empty; // "Group" or "OneOnOne"
    }

    // ─── GRADES (GradesController — read-only unified view) ──────

    public class GradeViewDto
    {
        public int Grade_ID { get; set; }
        public string Assessment_Name { get; set; } = string.Empty;
        public string Assessment_Type { get; set; } = string.Empty; // Quiz or Assignment
        public decimal Score { get; set; }
        public decimal Total_Points { get; set; }
        public decimal Percentage { get; set; }
        public string? Feedback { get; set; }
        public DateTime Grade_Date { get; set; }
    }

    // ─── QUIZ SUBMISSION (unused — QuizzesController.SubmitQuiz actually takes QuizSubmitDto/
    // StudentAnswerDto further below; nothing in the codebase references these two) ──
    public class QuizSubmissionDto
    {
        public int Student_ID { get; set; }
        public List<QuizAnswerDto> Answers { get; set; } = new();
    }

    public class QuizAnswerDto
    {
        public int Question_ID { get; set; }
        public string Selected_Answer { get; set; } = string.Empty;
    }

    // ─── ASSIGNMENT SUBMISSION (AssignmentsController — read-only view) ──

    // Response shape for AssignmentsController.GetAssignmentSubmissions
    public class AssignmentSubmissionViewDto
    {
        public int Submission_ID { get; set; }
        public int Assignment_ID { get; set; }
        public int Student_ID { get; set; }
        public string File_Name { get; set; } = string.Empty;
        public DateTime Submission_Date { get; set; }
        public decimal? Grade { get; set; }
        public string? Feedback { get; set; }
    }

    // ─── ANNOUNCEMENTS (AnnouncementsController) ──────────────────

    // Request body for AnnouncementsController.CreateAnnouncement
    public class AnnouncementCreateDto
    {
        [Required]
        [StringLength(200)]
        public string Announcement_Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Announcement_Details { get; set; } = string.Empty;

        [StringLength(50)]
        public string Announcement_Type { get; set; } = "Update";

        public int? Tutor_ID { get; set; }
        public int? Admin_ID { get; set; }

        [StringLength(20)]
        public string Module_Code { get; set; } = string.Empty;
    }

    // Request body for AnnouncementsController.UpdateAnnouncement
    public class AnnouncementUpdateDto
    {
        [Required]
        [StringLength(200)]
        public string Announcement_Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Announcement_Details { get; set; } = string.Empty;

        [StringLength(50)]
        public string Announcement_Type { get; set; } = "Update";

        [StringLength(20)]
        public string Module_Code { get; set; } = string.Empty;
    }

    // ─── BOOKING SLOTS & BOOKINGS (BookingSlotsController / BookingsController) ──

    // Request body for BookingSlotsController.CreateSlot/UpdateSlot
    public class BookingSlotCreateDto
    {
        public DateOnly Slot_Date { get; set; }
        public TimeOnly Slot_Time { get; set; }

        [Required]
        [StringLength(50)]
        public string Session_Type { get; set; } = string.Empty;

        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid tutor must be specified.")]
        public int Tutor_ID { get; set; }

        [Range(1, 1000, ErrorMessage = "Max capacity must be between 1 and 1000.")]
        public int? Max_Capacity { get; set; }

        [StringLength(20)]
        public string? Module_Code { get; set; }
    }

    // Request body for BookingsController.BookSession
    public class BookingCreateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid student must be selected.")]
        public int Student_ID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid booking slot must be selected.")]
        public int Booking_Slot_ID { get; set; }
    }

    // ─── ASSIGNMENTS (AssignmentsController) ──────────────────────

    // Request body for AssignmentsController.GradeSubmission
    public class GradeSubmissionDto
    {
        [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
        public decimal? Grade { get; set; }

        [StringLength(1000)]
        public string? Feedback { get; set; }
    }

    // Request body for AssignmentsController.CreateAssignment/UpdateAssignment
    public class AssignmentCreateDto
    {
        [Required(ErrorMessage = "Assignment name is required.")]
        [StringLength(200, ErrorMessage = "Assignment name cannot exceed 200 characters.")]
        public string Assignment_Name { get; set; } = string.Empty;

        public DateTime Assignment_Date { get; set; }

        [Required(ErrorMessage = "Module code is required.")]
        public string Module_Code { get; set; } = string.Empty;

        public string Assignment_URL { get; set; } = string.Empty;
    }

    // ─── QUIZZES (QuizzesController) ───────────────────────────────

    // Request body for QuizzesController.CreateQuiz/UpdateQuiz
    public class QuizCreateDto
    {
        [Required(ErrorMessage = "Quiz name is required.")]
        [StringLength(200, ErrorMessage = "Quiz name cannot exceed 200 characters.")]
        public string Quiz_Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Quiz_Details { get; set; } = string.Empty;

        public DateTime Quiz_Date { get; set; }

        [Required(ErrorMessage = "Module code is required.")]
        public string Module_Code { get; set; } = string.Empty;
    }

    // Request body for QuizzesController.SaveQuizQuestions — one question, with its options
    public class QuizQuestionSaveDto
    {
        [Required(ErrorMessage = "Question text is required.")]
        [StringLength(1000, ErrorMessage = "Question text cannot exceed 1000 characters.")]
        public string Question_Text { get; set; } = string.Empty;

        public int Question_Order { get; set; }

        [MinLength(2, ErrorMessage = "Each question must have at least 2 options.")]
        public List<QuizOptionSaveDto> Options { get; set; } = new();
    }

    // One answer option within a QuizQuestionSaveDto
    public class QuizOptionSaveDto
    {
        [Required(ErrorMessage = "Option text is required.")]
        [StringLength(500, ErrorMessage = "Option text cannot exceed 500 characters.")]
        public string Option_Text { get; set; } = string.Empty;

        public bool Is_Correct { get; set; }
    }

    // Request body for QuizzesController.SubmitQuiz — the actually-used pair (see the "unused" note above)
    public class QuizSubmitDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid student must be selected.")]
        public int Student_ID { get; set; }

        [MinLength(1, ErrorMessage = "At least one answer is required.")]
        public List<StudentAnswerDto> Answers { get; set; } = new();
    }

    public class StudentAnswerDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid question must be selected.")]
        public int Question_ID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid answer option must be selected.")]
        public int Option_ID { get; set; }
    }
}