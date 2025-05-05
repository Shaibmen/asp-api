using System.ComponentModel.DataAnnotations;

namespace ASPNETKEK.Models
{
    public class ReviewFormModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public string Text { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }
    }
}
