using System.IO;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IStorageService
    {
        Task<Stream> GetFileAsync(string fileRelativePath);
        string GetFileUrl(string fileRelativePath);
    }
}
