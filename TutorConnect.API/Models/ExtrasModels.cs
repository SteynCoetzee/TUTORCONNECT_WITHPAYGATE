// Extras: Models not directly tied to a specific iteration use case

using System.ComponentModel.DataAnnotations;

namespace TutorConnect.API.Models
{
    public class Notification
    {
        [Key]
        public int Notification_ID { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Date_Sent { get; set; } = DateTime.UtcNow;
        public bool Is_Read { get; set; } = false;

        // Links to the user receiving the notification
        public int User_ID { get; set; }
        public User? User { get; set; }
    }

    public class Audit_Log
    {
        [Key]
        public int Audit_Log_ID { get; set; }
        public DateOnly Audit_Date { get; set; }
        public TimeOnly Audit_Time { get; set; }
        public int User_ID { get; set; }
        public string Transaction_Type { get; set; } = string.Empty; // e.g., "Deleted Module", "Booked Session"
        public string Critical_Data { get; set; } = string.Empty; // e.g., "Module_Code: INF370"
    }

    // 7. Resource Types (Lookup table)
    public class Resource_Type
    {
        [Key]
        public int Resource_Type_ID { get; set; }
        public string Type_Name { get; set; } = string.Empty;
    }

    // 8. Business Rules
    public class Business_Rule
    {
        [Key]
        public int Rule_ID { get; set; }
        public string Rule_Name { get; set; } = string.Empty;
        public decimal Rule_Value { get; set; }
    }

    // Per-role sidebar/navbar visibility — "Admin", "Tutor", and "Student" rows exist.
    // AW-Tutor navigation stays hardcoded and is never stored here.
    public class Role_Nav_Setting
    {
        [Key]
        public int Role_Nav_Setting_ID { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Hidden_Items { get; set; } = string.Empty; // CSV of hidden item keys; "" = everything visible
    }

    // Per-user sidebar/navbar visibility override — a row here takes precedence over the user's
    // Role_Nav_Setting default. Most users never get a row; only ones an admin has customized.
    public class User_Nav_Setting
    {
        [Key]
        public int User_Nav_Setting_ID { get; set; }
        public int User_ID { get; set; }
        public string Hidden_Items { get; set; } = string.Empty; // CSV of hidden item keys
    }

    public class Module_Wishlist
    {
        [Key]
        public int Wishlist_ID { get; set; }
        public string Module_Code { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public int Student_ID { get; set; }
        public DateTime Date_Submitted { get; set; } = DateTime.UtcNow;
    }
}
