using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;

namespace LMS.DAL.Repositories
{
    public class LanguageRepository : Repository<int, Language>, ILanguageRepository
    {
        public LanguageRepository(LMSDBContext context) : base(context)
        {
        }
    }
}
