using LMS.DAL.Data;
using LMS.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        protected readonly LMSDBContext _context;

        public Repository(LMSDBContext context)
        {
            _context = context;
        }

        public async Task<T> Create(T t)
        {
            await _context.Set<T>().AddAsync(t);
            await _context.SaveChangesAsync();
            return t;
        }

        public async Task<T?> Get(K k)
        {
            return await _context.Set<T>().FindAsync(k);
        }

        public IEnumerable<Task<T?>> GetAll()
        {
            var list = _context.Set<T>().ToList();
            return list.Select(item => Task.FromResult<T?>(item));
        }

        public async Task<T?> Update(T t)
        {
            _context.Set<T>().Update(t);
            await _context.SaveChangesAsync();
            return t;
        }

        public async Task<T?> Delete(T t)
        {
            _context.Set<T>().Remove(t);
            await _context.SaveChangesAsync();
            return t;
        }
    }
}
