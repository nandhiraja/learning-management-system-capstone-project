
using System.ComponentModel.DataAnnotations;

namespace LMS.Core.Models
{
     public class ProgressRequestDto
    {
        [Required]
        [Range(0, 1000000)]
        public int WatchedSeconds { get; set; }

        [Required]
        public bool IsCompleted { get; set; }
    }
}