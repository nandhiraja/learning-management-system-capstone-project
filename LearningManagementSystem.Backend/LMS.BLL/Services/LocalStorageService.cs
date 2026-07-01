using System.IO;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;

namespace LMS.BLL.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly string _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        public async Task<Stream> GetFileAsync(string fileRelativePath)
        {
            var fullPath = Path.Combine(_rootPath, fileRelativePath.TrimStart('/'));
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The requested media file was not found.", fullPath);
            }
            return await Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public string GetFileUrl(string fileRelativePath)
        {
            // The fileUrl will route through the media controller securely
            return $"/api/media/stream?path={fileRelativePath.TrimStart('/')}";
        }
    }
}
