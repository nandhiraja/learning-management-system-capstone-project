using System;

namespace LMS.Core.Models
{
    public class LectureTranscript
    {
        public int Id { get; set; }
        public int LectureId { get; set; }
        
        public string Text { get; set; } = null!;
        public double StartTime { get; set; } // in seconds
        public double EndTime { get; set; }   // in seconds

        // Navigation properties
        public Lecture Lecture { get; set; } = null!;
    }
}
