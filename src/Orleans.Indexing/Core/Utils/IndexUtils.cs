using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Indexing.Facet;

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

        internal static bool IsNullable(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

        internal static TGrainState SetNullValues<TGrainState>(TGrainState state, IReadOnlyDictionary<string, object> propertyNullValues)
        {
            foreach (var propInfo in typeof(TGrainState).GetProperties())
            {
                var nullValue = GetNullValue(propInfo);
                if (nullValue != null || propertyNullValues.TryGetValue(propInfo.Name, out nullValue))
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

        internal static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> mutator)
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

        internal static void AddRange<T>(this HashSet<T> set, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                set.Add(value);
            }
        }

        internal static IEnumerable<T> Coalesce<T>(this IEnumerable<T> items)
        {
            return items == null
                ? Enumerable.Empty<T>()
                : from item in items where item != null select item;
        }

        internal static int GetInvariantHashCode(this object item)
            => (item is string stringItem) ? GetInvariantStringHashCode(stringItem) : item.GetHashCode();

        internal static int GetInvariantStringHashCode(this string item)
        {
            // NetCore randomizes string.GetHashCode() per-appdomain, to prevent hash flooding.
            // Therefore it's important to verify for each call site that this isn't a concern.
            // This is a non-unsafe/unchecked version of (internal) string.GetLegacyNonRandomizedHashCode().
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (var ii = 0; ii < item.Length; ii += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ item[ii];
                    if (ii < item.Length - 1)
                    {
                        hash2 = ((hash2 << 5) + hash2) ^ item[ii + 1];
                    }
                }
                return hash1 + (hash2 * 1566083941);
            }
        }

        internal static IndexScheme GetIndexScheme(this Type grainClassType)
        {
            IndexScheme? scheme = null;

            void setScheme(IndexScheme currentScheme)
                => scheme = scheme.HasValue && scheme.Value != currentScheme
                                ? throw new IndexConfigurationException($"Grain type {grainClassType.Name} has a conflict between indexing schemes specified on facet ctor parameters")
                                : currentScheme;

            foreach (var ctor in grainClassType.GetConstructors())
            {
                var ctorHasFacet = false;
                foreach (var attr in ctor.GetParameters().SelectMany(p => p.GetCustomAttributes<Attribute>()))
                {
                    if (ctorHasFacet)
                    {
                        throw new IndexConfigurationException($"Grain type {grainClassType.Name}: a ctor cannot have two Indexing facet specifications");
                    }
                    ctorHasFacet = true;
                    if (attr is IFaultTolerantWorkflowIndexWriterAttribute)
                    {
                        setScheme(IndexScheme.FaultTolerantWorkflow);
                    }
                    else if (attr is INonFaultTolerantWorkflowIndexWriterAttribute)
                    {
                        setScheme(IndexScheme.NonFaultTolerantWorkflow);
                    }
                    // TODO: Transactional
                }
            }

            return scheme ?? throw new IndexConfigurationException($"Grain type {grainClassType.Name} has no Indexing Facet constructor argument specified");
        }

        internal static bool IsIndexInterfaceType(this Type indexType)
            => typeof(IIndexInterface).IsAssignableFrom(indexType);

        internal static bool RequireIndexInterfaceType(this Type indexType)
            => indexType.IsIndexInterfaceType() ? true : throw new ArgumentException($"Type {GetFullTypeName(indexType)} is not an index type", "indexType");

        internal static bool IsPartitionedPerSiloIndex(this Type indexType)
            => indexType.RequireIndexInterfaceType() && typeof(IActiveHashIndexPartitionedPerSilo).IsAssignableFrom(indexType);

        internal static bool IsTotalIndex(this Type indexType)
            => indexType.RequireIndexInterfaceType() && typeof(ITotalIndex).IsAssignableFrom(indexType); // TODO Possible addition for Transactional

        internal static bool IsDirectStorageManagedIndex(this Type indexType)
            => indexType.RequireIndexInterfaceType() && typeof(IDirectStorageManagedIndex).IsAssignableFrom(indexType);

        internal static bool IsActiveIndex(this Type indexType)
            => !indexType.IsTotalIndex() && !indexType.IsDirectStorageManagedIndex();

        internal static bool IsTotalIndex(this IIndexInterface itf)
            => itf is ITotalIndex;     // TODO Possible addition for Transactional

        internal static bool IsDirectStorageManagedIndex(this IIndexInterface itf)
            => itf is IDirectStorageManagedIndex;

        internal static bool IsActiveIndex(this IIndexInterface itf)
            => !itf.IsTotalIndex() && !itf.IsDirectStorageManagedIndex();

        internal static bool IsActivationChange(this IndexUpdateReason updateReason)
            => updateReason == IndexUpdateReason.OnActivate || updateReason == IndexUpdateReason.OnDeactivate;
    }
}
