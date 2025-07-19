using System;
using System.Collections;

namespace FlammAlpha.UnityTools.Common
{
    /// <summary>
    /// Utility for counting and analyzing collection values.
    /// </summary>
    public static class CollectionCountUtility
    {
        /// <summary>
        /// Increments a count based on the value type and content.
        /// </summary>
        /// <param name="value">The value to analyze</param>
        /// <param name="counts">The count array to update</param>
        /// <param name="index">The index in the count array to increment</param>
        public static void IncrementCountForValue(object value, int[] counts, int index)
        {
            if (counts == null || index < 0 || index >= counts.Length)
                return;

            if (value is ICollection collection)
            {
                counts[index] += collection.Count;
            }
            else if (value != null)
            {
                // Non-null, non-collection values count as 1
                counts[index] += 1;
            }
            // Null values don't increment the count
        }

        /// <summary>
        /// Checks if a value should be considered "active" for highlighting purposes.
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>True if the value should trigger highlighting</returns>
        public static bool IsValueActive(object value)
        {
            if (value == null) return false;
            
            // Boolean values are active if true
            if (value is bool b) return b;
            
            // Collections are active if they have items
            if (value is ICollection collection) return collection.Count > 0;
            
            // Other non-null values are considered active
            return true;
        }

        /// <summary>
        /// Gets a summary string for a value (useful for debugging or UI display).
        /// </summary>
        /// <param name="value">The value to summarize</param>
        /// <returns>A string summary of the value</returns>
        public static string GetValueSummary(object value)
        {
            if (value == null) return "null";
            
            if (value is bool b) return b.ToString().ToLower();
            
            if (value is ICollection collection)
                return $"{collection.Count} items";
            
            return value.GetType().Name;
        }
    }
}
