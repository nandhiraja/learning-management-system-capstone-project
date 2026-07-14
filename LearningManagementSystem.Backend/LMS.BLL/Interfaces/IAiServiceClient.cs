using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public class TranscriptSegmentDto
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string Text { get; set; } = null!;
    }

    public interface IAiServiceClient
    {
        Task<List<TranscriptSegmentDto>> TranscribeAudioAsync(string audioFilePath);
    }
}
