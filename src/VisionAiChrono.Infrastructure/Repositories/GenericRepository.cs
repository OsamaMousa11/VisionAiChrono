using CleanArchitectureTemplate_Domain.Common;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using CleanArchitectureTemplate_infrastructure.Data;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
                
        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> GetTable()
        {
            return _dbSet;
        }

        #region Apply Includes
        protected IQueryable<T> ApplyIncludes(
            IQueryable<T> query,
            string? includeProperties)
        {
            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var includeProperty in includeProperties
                    .Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }
            return query;
        }
        #endregion          

        #region Get / Find

        public virtual async Task<T?> GetByIdAsync(Guid id, string? includeProperties = null, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();
            query = ApplyIncludes(query, includeProperties);

            return await query.FirstOrDefaultAsync(
                e => e.Id == id,
                cancellationToken);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(string? includeProperties = null, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();
            query = ApplyIncludes(query, includeProperties);

            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, string? includeProperties = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();
            query = ApplyIncludes(query, includeProperties);

            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public virtual async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, string? includeProperties = null, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet
                .Where(predicate)
                .AsNoTracking();

            query = ApplyIncludes(query, includeProperties);

            return await query.ToListAsync(cancellationToken);
        }

        #endregion

        #region Add

        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddRangeAsync(entities, cancellationToken);
        }

        #endregion

        #region Update


        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }


        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        #endregion

        #region Delete

        public virtual void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void DeleteRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        #endregion

        #region Utilities

        public virtual async Task<int> CountAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            return predicate == null
                ? await _dbSet.CountAsync(cancellationToken)
                : await _dbSet.CountAsync(predicate, cancellationToken);
        }

        public virtual async Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }

        #endregion

        #region Paging

        public virtual async Task<IEnumerable<T>> GetPagedAsync(
            int pageNumber = 1,
            int pageSize = 10,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string? includeProperties = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            query = ApplyIncludes(query, includeProperties);

            if (orderBy != null)
                query = orderBy(query);

            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<(IEnumerable<T> Data, int TotalCount)> GetPagedWithCountAsync(int pageNumber = 1, int pageSize = 10, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string? includeProperties = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
