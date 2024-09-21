using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Interfaces
{
    public enum ErrorCode
    {
        Success,
        Error
    }

    public interface IBaseRepository<T>
    {
        T Get(object id);

        List<T> GetAll();
        ErrorCode Create(T t);
        ErrorCode Update(object id, T t);
        ErrorCode Delete(object id);

        IEnumerable<T> FindByCondition(Expression<Func<T, bool>> expression);
    }

}
