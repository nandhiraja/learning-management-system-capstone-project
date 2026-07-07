using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICertificatePdfGenerator
    {
        byte[] GenerateCertificatePdf(string studentName, string courseName, string instructorName, string dateIssued, string certificateId, int nameChangesCount);
    }
}
