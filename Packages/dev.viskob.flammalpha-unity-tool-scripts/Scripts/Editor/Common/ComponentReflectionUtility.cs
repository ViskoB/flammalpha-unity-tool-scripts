using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FlammAlpha.UnityTools.Common
{
    /// <summary>
    /// Utility for safely accessing component properties and fields via reflection.
    /// Handles problematic properties that create instances in edit mode.
    /// </summary>
    public static class ComponentReflectionUtility
    {
        /// <summary>
        /// Safely gets a property or field value from a component, automatically handling problematic properties.
        /// </summary>
        /// <param name="component">The component instance</param>
        /// <param name="componentType">The component type</param>
        /// <param name="memberName">The property or field name</param>
        /// <returns>The value, or null if the member doesn't exist or is problematic without a safe alternative</returns>
        public static object GetComponentValue(object component, Type componentType, string memberName)
        {
            if (component == null || componentType == null || string.IsNullOrEmpty(memberName))
                return null;

            // Use PropertySafetyUtility to safely handle problematic properties
            if (FlammAlpha.UnityTools.Hierarchy.Highlight.PropertySafetyUtility.IsProblematicProperty(componentType, memberName))
            {
                // Get the safe alternative property name
                string safeAlternative = FlammAlpha.UnityTools.Hierarchy.Highlight.PropertySafetyUtility.GetSafeAlternative(componentType, memberName);
                if (!string.IsNullOrEmpty(safeAlternative))
                {
                    // Use the safe alternative instead
                    var safeProp = componentType.GetProperty(safeAlternative, BindingFlags.Public | BindingFlags.Instance);
                    if (safeProp != null) return safeProp.GetValue(component);
                    var safeField = componentType.GetField(safeAlternative, BindingFlags.Public | BindingFlags.Instance);
                    if (safeField != null) return safeField.GetValue(component);
                }
                // If no safe alternative exists, return null to avoid creating instances
                return null;
            }

            // Try property first, then field
            var prop = componentType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null) return prop.GetValue(component);
            var field = componentType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(component);

            return null;
        }

        /// <summary>
        /// Gets all public properties of a type that match certain criteria.
        /// </summary>
        /// <param name="type">The type to inspect</param>
        /// <param name="includeCollections">Include collection properties (arrays, lists)</param>
        /// <param name="includeMaterials">Include Material/Texture properties</param>
        /// <param name="includeBooleans">Include boolean properties</param>
        /// <param name="filterProblematic">Filter out problematic properties</param>
        /// <returns>Array of property names</returns>
        public static string[] GetPropertyNames(Type type, bool includeCollections = true, bool includeMaterials = true, bool includeBooleans = true, bool filterProblematic = true)
        {
            if (type == null) return new string[0];

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                {
                    // Include based on criteria
                    bool shouldInclude = false;

                    if (includeCollections && (
                        typeof(System.Collections.IList).IsAssignableFrom(p.PropertyType) ||
                        (p.PropertyType.IsGenericType && p.PropertyType.GetInterfaces().Any(x =>
                            x.IsGenericType && x.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IList<>)))))
                    {
                        shouldInclude = true;
                    }

                    if (includeMaterials && (
                        p.PropertyType == typeof(Material) ||
                        p.PropertyType == typeof(UnityEngine.Mesh) ||
                        p.PropertyType == typeof(Texture)))
                    {
                        shouldInclude = true;
                    }

                    if (includeBooleans && p.PropertyType == typeof(bool))
                    {
                        shouldInclude = true;
                    }

                    return shouldInclude;
                })
                .Where(p => !filterProblematic || !FlammAlpha.UnityTools.Hierarchy.Highlight.PropertySafetyUtility.IsProblematicProperty(type, p.Name))
                .Select(p => p.Name)
                .Distinct()
                .OrderBy(p => p)
                .ToArray();

            return props;
        }
    }
}
