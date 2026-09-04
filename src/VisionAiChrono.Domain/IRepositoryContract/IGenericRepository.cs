using CleanArchitectureTemplate_Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Domain.IRepositoryContract
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        IQueryable<T> GetTable();

        // ---------- Get ----------
        Task<T?> GetByIdAsync(Guid id, string? includeProperties = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> GetAllAsync(string? includeProperties = null, CancellationToken cancellationToken = default);

        Task<T?> FindAsync(Expression<Func<T, bool>> predicate, string? includeProperties = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, string? includeProperties = null, CancellationToken cancellationToken = default);

        // Add 
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        //  Update 
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);

        //  Delete 
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);

        //  Utilities 
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        //  Paging
        Task<IEnumerable<T>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 10,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string? includeProperties = null,
            CancellationToken cancellationToken = default
        );

        // New Paging Method with Total Count
        Task<(IEnumerable<T> Data, int TotalCount)> GetPagedWithCountAsync(
            int pageNumber = 1,
            int pageSize = 10,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string? includeProperties = null,
            CancellationToken cancellationToken = default
        );
    }
}
