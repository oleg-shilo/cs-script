// Ignore Spelling: Deconstruct

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CSScriptLib
{
    /// <summary>
    /// This class allows attaching arbitrary data to any object. This behavior resembles
    /// AttachedProperty in WPF.
    /// </summary>
    public static class AttachedProperties
    {
        /// <summary>
        /// The object cache. Contains object that have values attached.
        /// </summary>
        public static ConditionalWeakTable<object, Dictionary<string, object>> ObjectCache = new ConditionalWeakTable<object, Dictionary<string, object>>();

        /// <summary>
        /// Sets the attached value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void SetAttachedValue<T>(this T obj, string name, object value) where T : class
        {
            var key = name ?? value?.GetType().FullName;

            Dictionary<string, object> properties = ObjectCache.GetOrCreateValue(obj);

            if (properties.ContainsKey(key))
                properties[key] = value;
            else
                properties.Add(key, value);
        }

        /// <summary>
        /// Sets an attached value for the specified object of type T.
        /// </summary>
        /// <remarks>This method enables dynamic association of additional data with an object at runtime.
        /// If multiple values need to be attached, consider using an overload that accepts a key parameter.</remarks>
        /// <typeparam name="T">The type of the object to which the attached value is assigned. Must be a reference type.</typeparam>
        /// <param name="obj">The object to which the attached value is assigned. Cannot be null.</param>
        /// <param name="value">The value to attach to the specified object. This can be any object.</param>
        public static void SetAttached<T>(this object obj, object value)
            => obj.SetAttachedValue(typeof(T).FullName, value);

        /// <summary>
        /// Gets the attached value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static T GetAttachedValue<T>(this object obj, string name)
        {
            var key = name ?? typeof(T).FullName;
            Dictionary<string, object> properties;
            if (ObjectCache.TryGetValue(obj, out properties) && properties.ContainsKey(key))
                return (T)properties[key];
            else
                return default(T);
        }

        /// <summary>
        /// Retrieves the attached value of the specified type from the given object.
        /// </summary>
        /// <remarks>This method is an extension method that allows for easy retrieval of attached values
        /// without needing to know the specific implementation details of the object.</remarks>
        /// <typeparam name="T">The type of the attached value to retrieve.</typeparam>
        /// <param name="obj">The object from which to retrieve the attached value. This parameter cannot be null.</param>
        /// <returns>The attached value of type T, or the default value of T if no value is attached.</returns>
        public static T GetAttached<T>(this object obj)
            => obj.GetAttachedValue<T>(null);

        /// <summary>
        /// Gets the attached value.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static object GetAttachedValue(this object obj, string name)
        {
            return obj.GetAttachedValue<object>(name);
        }
    }
}