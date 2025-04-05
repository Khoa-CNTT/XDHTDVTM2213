using System.ComponentModel.DataAnnotations;

namespace FlightBookingApp.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }

        [Required]
        public int BookingId { get; set; }

        public string? CompanyName { get; set; } // Cho phép NULL

        public string? CompanyAddress { get; set; } // Cho phép NULL

        public string? CompanyCity { get; set; } // Cho phép NULL

        public string? TaxCode { get; set; } // Cho phép NULL

        public string? InvoiceRecipient { get; set; } // Cho phép NULL

        public string? InvoicePhone { get; set; } // Cho phép NULL

        public string? InvoiceEmail { get; set; } // Cho phép NULL

        public Booking Booking { get; set; }
    }
}