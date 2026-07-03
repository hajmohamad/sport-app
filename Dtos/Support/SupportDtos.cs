using System.ComponentModel.DataAnnotations;
using sport_app_backend.Models.Support; // اضافه کردن این using

namespace sport_app_backend.Dtos;

public class CreateTicketDto
{
    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string MessageText { get; set; } = string.Empty;
}

public class ReplyTicketDto
{
    [Required]
    public string MessageText { get; set; } = string.Empty;
}

public class TicketListDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public TicketCategory Category { get; set; } // بازگشت نوع اصلی Enum
    public TicketStatus Status { get; set; }     // بازگشت نوع اصلی Enum
    public string StatusRaw => Status.ToString(); // نام انگلیسی Enum برای راحتی کلاینت
    public DateTime LastUpdatedAt { get; set; }  // بازگشت شیء DateTime
    public DateTime CreatedAt { get; set; }
}

public class TicketMessageDto
{
    public int Id { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }      // بازگشت شیء DateTime
    public bool IsFromSupport { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderImage { get; set; } = string.Empty;
}

public class TicketDetailsDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public TicketCategory Category { get; set; } // بازگشت نوع اصلی Enum
    public TicketStatus Status { get; set; }     // بازگشت نوع اصلی Enum
    public string StatusRaw => Status.ToString();
    public List<TicketMessageDto> Messages { get; set; } = [];
}
public class AdminTicketListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public string StatusRaw => Status.ToString();
    public TicketCategory Category { get; set; }
    public string CategoryRaw => Category.ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
        
    // اطلاعات کاربر ایجاد کننده تیکت
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserPhoneNumber { get; set; } = string.Empty;
}
public class AdminTicketDetailsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public string StatusRaw => Status.ToString();
    public TicketCategory Category { get; set; }
    public string CategoryRaw => Category.ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
        
    // اطلاعات کاربر
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserPhoneNumber { get; set; } = string.Empty;

    // لیست پیام‌ها
    public List<TicketMessageDto> Messages { get; set; } = new();
}
