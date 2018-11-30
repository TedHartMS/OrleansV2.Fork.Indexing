using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    /// <summary>
    /// A utility class for the low-level operations related to indexes
    /// </summary>
    public static class IndexUtils
    {
        /// <summary>
        /// A utility function for getting the index grainID, which is a simple concatenation of the grain
        /// interface type and indexName
        /// </summary>
        /// <param name="grainType">the grain interface type</param>
        /// <param name="indexName">the name of the index, which is the identifier of the index</param>
        /// <returns>index grainID</returns>
        public static string GetIndexGrainPrimaryKey(Type grainType, string indexName)
            => string.Format("{0}-{1}", GetFullTypeName(grainType), indexName);

        /// <summary>
        /// This method extracts the name of an index grain from its primary key
        /// </summary>
        /// <param name="index">the given index grain</param>
        /// <returns>the name of the index</returns>
        public static string GetIndexNameFromIndexGrain(IAddressable index)
        {
            string key = index.GetPrimaryKeyString();
            return key.Substring(key.LastIndexOf("-") + 1);
        }

        internal static string GetNextIndexBucketIdInChain(IAddressable index)
        {
            string key = index.GetPrimaryKeyString();
            int next = 1;
            if (key.Split('-').Length == 3)
            {
                int lastDashIndex = key.LastIndexOf("-");
                next = int.Parse(key.Substring(lastDashIndex + 1)) + 1;
                return key.Substring(0, lastDashIndex + 1) + next;
            }
            return key + "-" + next;
        }

        /// <summary>
        /// This method is a central place for finding the indexes defined on a getter method of a given
        /// grain interface.
        /// </summary>
        /// <param name="propertyName">the name of the property on the grain interface</param>
        /// <returns>the name of the index on the getter method of the grain interface</returns>
        public static string PropertyNameToIndexName(string propertyName)
            => "__" + propertyName;

        public static System.Threading.Tasks.Task<bool> WriteIndexes<TGrainState>(TGrainState grainState, params object[] indexedProperties)
        {
            return System.Threading.Tasks.Task.FromResult(true);
        }

        // The ILoggerFactory implementation creates the category without generic type arguments.
        internal static ILogger CreateLoggerWithFullCategoryName<T>(this ILoggerFactory lf) where T: class
            => lf.CreateLoggerWithFullCategoryName(typeof(T));

        internal static ILogger CreateLoggerWithFullCategoryName(this ILoggerFactory lf, Type t)
            => lf.CreateLogger(GetFullTypeName(t, expandArgNames: true));

        internal static string GetFullTypeName(Type type, bool expandArgNames = false)
        {
            var name = type.FullName ?? (type.IsGenericParameter ? type.Name : type.Namespace + "." + type.Name);
            var assemblyInfoStart = name.IndexOf("[[");
            if (assemblyInfoStart > 0) name = name.Substring(0, assemblyInfoStart);
            var genericArgs = type.GetGenericArguments();
            return (genericArgs.Length == 0 || !expandArgNames)
                ? name
                : $"{name.Substring(0, name.IndexOf("`"))}<{string.Join(",", genericArgs.Select(arg => GetFullTypeName(arg, true)))}>";
        }

        internal static bool IsNullable(this Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        internal static TGrainState SetNullValues<TGrainState>(TGrainState state)
        {
            foreach (var propInfo in typeof(TGrainState).GetProperties())
            {
                var nullValue = GetNullValue(propInfo);
                if (nullValue != null)
                {
                    propInfo.SetValue(state, nullValue);
                }
            }
            return state;
        }

        internal static object GetNullValue(PropertyInfo propInfo)
        {
            if (propInfo.PropertyType.IsNullable())
            {
                return null;
            }
            var indexAttrs = propInfo.GetCustomAttributes<IndexAttribute>(inherit: false);
            var indexAttr = indexAttrs.FirstOrDefault(attr => !string.IsNullOrEmpty(attr.NullValue));
            return indexAttr == null || string.IsNullOrEmpty(indexAttr.NullValue)
                ? null
                : indexAttr.NullValue.ConvertTo(propInfo.PropertyType);
        }

        internal static object ConvertTo(this string value, Type propertyType)
        {
            return propertyType == typeof(DateTime)
                ? DateTime.ParseExact(value, "o", CultureInfo.InvariantCulture)
                : Convert.ChangeType(value, propertyType, CultureInfo.InvariantCulture);
        }

        internal static void Mutate<T>(this IEnumerable<T> enumerable, Action<T> mutator)
        {
            // Simple but allows chaining
            foreach (var item in enumerable)
            {
                mutator(item);
            }
        }

        internal static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> creatorFunc)
        {
            if (!dict.TryGetValue(key, out TValue value))
            {
                value = creatorFunc();
                dict[key] = value;
            }
            return value;
        }

        internal static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
            => GetOrAdd(dict, key, () => new TValue());


        internal static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value) where TValue : new()
            => GetOrAdd(dict, key, () => value);

        internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
