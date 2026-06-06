using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
