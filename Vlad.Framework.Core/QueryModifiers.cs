﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Aspectacular
{
    /// <summary>
    /// Defines common query modifiers
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IQueryModifier<TEntity>
    {
        /// <summary>
        /// Modifies given query in some way.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        IQueryable<TEntity> Augment(IQueryable<TEntity> query);

        IEnumerable<TEntity> Augment(IEnumerable<TEntity> query);
    }

    public static class QueryModifiersExtensions
    {
        /// <summary>
        /// Modifies a query by applying paging, sorting and filtering.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query">Query to modify.</param>
        /// <param name="optionalQueryModifiers">Modifications to be applied to the query</param>
        /// <returns></returns>
        public static IQueryable<TEntity> AugmentQuery<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            if(optionalQueryModifiers == null || optionalQueryModifiers.Length == 0)
                return query;

            return query == null ? null : optionalQueryModifiers.Where(mod => mod != null).Aggregate(query, (current, modifier) => modifier.Augment(current));
        }

        public static IQueryable<TEntity> AugmentQuery<TEntity>(this IQueryable<TEntity> query, CommonQueryModifiers queryModifiers)
        {
            IQueryModifier<TEntity>[] mods = queryModifiers.GetModifiers<TEntity>();
            return query.AugmentQuery(mods);
        }

        /// <summary>
        /// Modifies a collection by applying paging, sorting and filtering.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">Query to modify.</param>
        /// <param name="optionalQueryModifiers">Modifications to be applied to the query</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> AugmentQuery<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            if (optionalQueryModifiers == null || optionalQueryModifiers.Length == 0)
                return collection;

            return collection == null ? null : optionalQueryModifiers.Where(mod => mod != null).Aggregate(collection, (current, modifier) => modifier.Augment(current));
        }

        public static IEnumerable<TEntity> AugmentQuery<TEntity>(this IEnumerable<TEntity> collection, CommonQueryModifiers queryModifiers)
        {
            IQueryModifier<TEntity>[] mods = queryModifiers.GetModifiers<TEntity>();
            return collection.AugmentQuery(mods);
        }

        public static IList<TEntity> ToIListWithMods<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            var newQuery = query.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            IList<TEntity> retVal = newQuery as IList<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static List<TEntity> ToListWithMods<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            var newQuery = query.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            List<TEntity> retVal = newQuery as List<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static IList<TEntity> ToIListWithMods<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            var newQuery = collection.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            IList<TEntity> retVal = newQuery as IList<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static List<TEntity> ToListWithMods<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            var newQuery = collection.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            List<TEntity> retVal = newQuery as List<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static TEntity FirstOrDefaultWithMods<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers) where TEntity : class
        {
            var newQuery = query.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            TEntity retVal = newQuery as TEntity ?? newQuery.FirstOrDefault();

            return retVal;
        }

        public static TEntity FirstOrDefaultWithMods<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers) where TEntity : class
        {
            var newCollection = collection.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            TEntity retVal = newCollection as TEntity ?? newCollection.FirstOrDefault();

            return retVal;
        }

        public static TEntity SingleOrDefaultWithMods<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers) where TEntity : class
        {
            var newQuery = query.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            TEntity retVal = newQuery as TEntity ?? newQuery.SingleOrDefault();

            return retVal;
        }

        public static TEntity SingleOrDefaultWithMods<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers) where TEntity : class
        {
            var newCollection = collection.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            TEntity retVal = newCollection as TEntity ?? newCollection.SingleOrDefault();

            return retVal;
        }
    }

    #region Stock Query Modifier Classes

    /// <summary>
    /// Defines query paging modifier. 
    /// Modified query will bring only a subset of data 
    /// not exceeding in size the number of records specified by PageSize property.
    /// </summary>
    public class Paging<TEntity> : IQueryModifier<TEntity>
    {
        public Paging(int pageIndex, int pageSize)
        {
            if(pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex cannot be negative");
            if(pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize cannot less than 1");

            this.PageIndex = pageIndex;
            this.PageSize = pageSize;
        }

        public Paging() : this(pageIndex: 0, pageSize: 20)
        {
        }

        /// <summary>
        /// Zero-based page index;
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// Data page size in the number of records.
        /// </summary>
        public int PageSize { get; set; }

        IQueryable<TEntity> IQueryModifier<TEntity>.Augment(IQueryable<TEntity> query)
        {
            int skipCount = this.CalcSkip();
            return query.Skip(skipCount).Take(this.PageSize);
        }

        IEnumerable<TEntity> IQueryModifier<TEntity>.Augment(IEnumerable<TEntity> query)
        {
            int skipCount = this.CalcSkip();
            return query.Skip(skipCount).Take(this.PageSize);
        }

        private int CalcSkip()
        {
            if (this.PageIndex < 0)
                throw new ArgumentException("PageIndex parameter cannot be negative.");
            if (this.PageSize < 1)
                throw new ArgumentException("PageSize parameter must be greater than 0.");

            return this.PageIndex * this.PageSize;
        }
    }

    public abstract class MemberAugmentBase<TEntity, TKey> : IQueryModifier<TEntity>
    {
        public Expression<Func<TEntity, TKey>> EntitySortProperty { get; set; }

        protected MemberAugmentBase(Expression<Func<TEntity, TKey>> sortProperty)
        {
            this.EntitySortProperty = sortProperty;
        }

        //protected MemberAugmentBase(string sortPropertyName) : this(MemberExpression.Property())
        //{

        //}

        protected MemberAugmentBase()
            : this(sortProperty: null)
        {
        }

        IQueryable<TEntity> IQueryModifier<TEntity>.Augment(IQueryable<TEntity> query)
        {
            if (query == null)
                return null;

            query = this.Augment(query, this.EntitySortProperty);

            return query;
        }

        protected abstract IQueryable<TEntity> Augment(IQueryable<TEntity> query, Expression<Func<TEntity, TKey>> expression);

        IEnumerable<TEntity> IQueryModifier<TEntity>.Augment(IEnumerable<TEntity> collection)
        {
            if (collection == null)
                return null;

            collection = this.AugmentCollection(collection, this.EntitySortProperty.Compile());

            return collection;
        }

        protected abstract IEnumerable<TEntity> AugmentCollection(IEnumerable<TEntity> collection, Func<TEntity, TKey> func);
    }

    /// <summary>
    /// Modifies query by adding ascending sorting for a given field.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class SortingAsc<TEntity, TKey> : MemberAugmentBase<TEntity, TKey>
    {
        public SortingAsc(Expression<Func<TEntity, TKey>> sortProperty) : base(sortProperty)
        {
        }

        public SortingAsc()
        {
        }

        protected override IQueryable<TEntity> Augment(IQueryable<TEntity> query, Expression<Func<TEntity, TKey>> sortPropertyExpression)
        {
            return query.OrderBy(sortPropertyExpression);
        }

        protected override IEnumerable<TEntity> AugmentCollection(IEnumerable<TEntity> collection, Func<TEntity, TKey> sortFunc)
        {
            return collection.OrderBy(sortFunc);
        }
    }

    public class SortingDesc<TEntity, TKey> : MemberAugmentBase<TEntity, TKey>
    {
        public SortingDesc(Expression<Func<TEntity, TKey>> sortProperty)
            : base(sortProperty)
        {
        }

        public SortingDesc()
        {
        }

        protected override IQueryable<TEntity> Augment(IQueryable<TEntity> query, Expression<Func<TEntity, TKey>> sortPropertyExpression)
        {
            return query.OrderByDescending(sortPropertyExpression);
        }

        protected override IEnumerable<TEntity> AugmentCollection(IEnumerable<TEntity> collection, Func<TEntity, TKey> sortFunc)
        {
            return collection.OrderByDescending(sortFunc);
        }
    }

    public enum SortOrder
    {
        Asceding, Descending
    }

    /// <summary>
    /// Allows using List(), Single() and other AOP wire-tripping functions 
    /// to apply query augmentation to DAL methods returning IQueryable.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class QueryFilter<TEntity> : IQueryModifier<TEntity>
    {
        public Expression<Func<TEntity, bool>> QueryablePredicate { get; set; }
        public Func<TEntity, bool> EnumerablePredicate { get; set; }

        public QueryFilter(Expression<Func<TEntity, bool>> predicate)
        {
            this.QueryablePredicate = predicate;
        }

        public QueryFilter(Func<TEntity, bool> predicate)
        {
            this.EnumerablePredicate = predicate;
        }

        public QueryFilter()
        {
        }

        IQueryable<TEntity> IQueryModifier<TEntity>.Augment(IQueryable<TEntity> query)
        {
            return query.Where(this.QueryablePredicate);
        }

        IEnumerable<TEntity> IQueryModifier<TEntity>.Augment(IEnumerable<TEntity> query)
        {
            if(this.EnumerablePredicate == null && this.QueryablePredicate != null)
                this.EnumerablePredicate = this.QueryablePredicate.Compile();

            if (this.EnumerablePredicate == null)
                throw new NullReferenceException("EnumerablePredicate must be specified.");

            return query.Where(this.EnumerablePredicate);
        }
    }

    #endregion Stock Query Modifier Classes

    /// <summary>
    /// Web-service-friendly XML- and JSON-serializable structure 
    /// defining common query modifiers.
    /// </summary>
    public class CommonQueryModifiers
    {
        #region Inner structures

        public class PagingInfo
        {
            public int PageIndex { get; set; }
            public int PageSize { get; set; }

            internal IQueryModifier<TEntity> GetModifier<TEntity>()
            {
                return new Paging<TEntity>(this.PageIndex, this.PageSize);
            }
        }

        public class FilterInfo
        {
            public string FilterColumnName { get; set; }
            public object FilterValue { get; set; }
            public DynamicFilterOperators FilterOperator { get; set; }

            internal IQueryModifier<TEntity> GetModifier<TEntity>()
            {
                Expression<Func<TEntity, bool>> predicate = PredicateBuilder.GetPredicate<TEntity>(this.FilterColumnName, this.FilterOperator, this.FilterValue);
                QueryFilter<TEntity> filter = new QueryFilter<TEntity>(predicate);
                return filter;
            }
        }
        // ReSharper restore PossiblyMistakenUseOfParamsMethod

        public class SortingInfo
        {
            public SortOrder SortOrder { get; set; }
            public string SortFieldName { get; set; }

            internal IQueryModifier<TEntity> GetModifier<TEntity>()
            {
                //Expression<Func<TEntity, TKey>> sortProperty = ...
                //return this.SortOrder == SortOrder.Asceding ? new SortingAsc<TEntity>()
                throw new NotImplementedException("SortingInfo.GetModifier()");
            }
        }

        #endregion Inner structures

        /// <summary>
        /// Filters combined by "and" operator.
        /// </summary>
        public List<FilterInfo> Filters { get; set; }

        public List<SortingInfo> Sorting { get; set; }

        public PagingInfo Paging { get; set; }
    }

    internal static partial class InternalExtensions
    {
        internal static IQueryModifier<TEntity>[] GetModifiers<TEntity>(this CommonQueryModifiers cmod)
        {
            if(cmod == null)
                return null;

            List<IQueryModifier<TEntity>> modifiers = new List<IQueryModifier<TEntity>>();

            if(cmod.Filters != null)
                modifiers.AddRange(cmod.Filters.Where(f => f != null).Select(f => f.GetModifier<TEntity>()));

            if(cmod.Sorting != null)
                modifiers.AddRange(cmod.Sorting.Where(s => s != null).Select(s => s.GetModifier<TEntity>()));

            if(cmod.Paging != null)
                modifiers.Add(cmod.Paging.GetModifier<TEntity>());

            return modifiers.Count == 0 ? null : modifiers.ToArray();
        }
    }
}
