using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.DAL.Interfaces
{
    public interface IRepository<K,T> where T: class
    {
        public Task<T> Create(T t);
        public Task<T?> Get(K k);
        public IEnumerable<Task<T?>> GetAll();
        public Task<IEnumerable<T>> GetAllAsync();
        public Task<T?> Update(T t);
        public Task<T?> Delete(T t);

    }
}