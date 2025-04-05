using System.ComponentModel.DataAnnotations;

namespace FlightBookingApp.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public string? PaymentMethod { get; set; } // Cho phép NULL

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public string? Status { get; set; } // Cho phép NULL

        public Booking Booking { get; set; }
    }
}