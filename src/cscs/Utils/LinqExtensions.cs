using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSScripting
{
    /// <summary>
    /// Various LINQ extensions
    /// </summary>
    public static class LinqExtensions
    {
        internal static List<T> AddIfNotThere<T>(this List<T> items, T item)
        {
            if (!items.Contains(item))
                items.Add(item);
            return items;
        }

        /// <summary>
        /// None of the items matches the specified predicate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>Result of the test</returns>
        public static bool None<T>(this IEnumerable<T> items, Func<T, bool> predicate) => !items.Any(predicate);

        /// <summary>
        /// Adds a single item to the collection.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="item">The item.</param>
        /// <returns>The original collection instance.</returns>
        public static IEnumerable<TSource> AddItem<TSource>(this IEnumerable<TSource> items, TSource item) =>
            items.Concat(new[] { item });

        /// <summary>
        /// Determines whether the collection is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns>
        ///   <c>true</c> if the specified collection is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmpty<T>(this IEnumerable<T> collection) => collection == null ? true : !collection.Any();

        internal static string[] Distinct(this string[] list) => Enumerable.Distinct(list).ToArray();

        internal static string[] ConcatWith(this string[] array1, IEnumerable<string> array2) =>
            array1.Concat(array2).ToArray();

        internal static string[] ConcatWith(this string[] array, string item) =>
            array.Concat(new[] { item }).ToArray();

        internal static string[] ConcatWith(this string item, IEnumerable<string> array)
            => new[] { item }.Concat(array).ToArray();

        /// <summary>
        /// A generic LINQ equivalent of C# foreach loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action.</param>
        /// <returns>The original collection instance</returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T item in collection)
            {
                action(item);
            }
            return collection;
        }

        /// <summary>
        /// Allows updating the object in Fluent expressions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object">The object.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static T With<T>(this T @object, Action<T> action)
        {
            action(@object);
            return @object;
        }

        internal static (T value1, T value2) ToTupleOf2<T>(this T[] array)
            => (array.FirstOrDefault(), array.Skip(1).FirstOrDefault());

        internal static T2 With1<T, T2>(this T @object, Func<T, T2> action)
        {
            return action(@object);
        }
    }
}