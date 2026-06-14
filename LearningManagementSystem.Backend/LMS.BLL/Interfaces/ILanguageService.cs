using LMS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ILanguageService
    {
        Task<IEnumerable<Language>> GetLanguagesAsync(bool onlyApproved = true);
        Task<Language?> GetLanguageByIdAsync(int id);
        Task<Language> CreateLanguageAsync(string name, bool isApproved);
        Task<bool> UpdateLanguageAsync(int id, string name);
        Task<bool> DeleteLanguageAsync(int id);
        Task<bool> ApproveLanguageAsync(int id);
    }
}
