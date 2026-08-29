using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.DTOs
{
    // --- PAYMENT DTOs (manual EFT/Ozow flow — see PaymentsController, not PayFastController) ---

    // Request body for PaymentsController.MakePayment
    public class PaymentCreateDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? Account_Name { get; set; }

        [StringLength(20)]
        public string? Account_Number { get; set; }

        [StringLength(10)]
        public string? Branch_Code { get; set; }

        [StringLength(50)]
        public string? Bank { get; set; }

        [StringLength(100)]
        public string? Payment_Reference { get; set; }

        public int Student_ID { get; set; }

        [Required]
        [StringLength(20)]
        public string Module_Code { get; set; } = string.Empty;
    }

    // Response shape for PaymentsController.GetStudentPayments
    public class PaymentReturnDto
    {
        public int Payment_ID { get; set; }
        public decimal Amount { get; set; }
        public DateTime Payment_Date { get; set; }
        public string Payment_Status { get; set; } = string.Empty;
        public string? Bank { get; set; }
        public string? Payment_Reference { get; set; }
        public string Module_Code { get; set; } = string.Empty;
    }

    // --- NOTIFICATION DTOs ---

    // Response shape for NotificationsController.GetUserNotifications
    public class NotificationReturnDto
    {
        public int Notification_ID { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Date_Sent { get; set; }
        public bool Is_Read { get; set; }
    }
}