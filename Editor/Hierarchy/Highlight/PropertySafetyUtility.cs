using System;
using System.Linq;
using UnityEngine;

namespace FlammAlpha.UnityTools.Hierarchy.Highlight
{
    /// <summary>
    /// Utility class for checking if Unity component properties are safe to access in edit mode.
    /// Prevents material/mesh instance creation and other problematic behaviors.
    /// </summary>
    public static class PropertySafetyUtility
    {
        /// <summary>
        /// Checks if a property on a component type would cause issues when accessed in edit mode.
        /// Returns true for properties that create material instances, mesh instances, or other problematic behaviors.
        /// </summary>
        /// <param name="componentType">The component type containing the property</param>
        /// <param name="propertyName">The property name to check</param>
        /// <returns>True if the property would cause issues (like material instance creation)</returns>
        public static bool IsProblematicProperty(Type componentType, string propertyName)
        {
            if (componentType == null || string.IsNullOrEmpty(propertyName))
                return false;

            // Check for Renderer material properties that create instances in edit mode
            if (typeof(Renderer).IsAssignableFrom(componentType))
            {
                if (propertyName == "material" || propertyName == "materials")
                {
                    return true; // These create material instances when accessed
                }
            }

            // MeshFilter.mesh creates mesh instances
            if (typeof(MeshFilter).IsAssignableFrom(componentType))
            {
                if (propertyName == "mesh")
                {
                    return true; // This creates mesh instances when accessed
                }
            }

            // ParticleSystem module properties create instances
            if (componentType == typeof(ParticleSystem))
            {
                var problematicModules = new[] {
                    "main", "emission", "shape", "velocityOverLifetime", "limitVelocityOverLifetime",
                    "inheritVelocity", "forceOverLifetime", "colorOverLifetime", "colorBySpeed",
                    "sizeOverLifetime", "sizeBySpeed", "rotationOverLifetime", "rotationBySpeed",
                    "externalForces", "noise", "collision", "trigger", "subEmitters", 
                    "textureSheetAnimation", "lights", "trails"
                };
                
                if (problematicModules.Contains(propertyName))
                {
                    return true; // These create module instances when accessed
                }
            }

            // Add more problematic property patterns here as they are discovered
            // Examples of other Unity components that might have similar issues:
            
            // LineRenderer.materials creates instances (but materials is already covered above for Renderer)
            // TrailRenderer.materials creates instances (but materials is already covered above for Renderer)
            
            return false;
        }

        /// <summary>
        /// Gets the safe alternative property name for a problematic property, if one exists.
        /// </summary>
        /// <param name="componentType">The component type containing the property</param>
        /// <param name="problematicPropertyName">The problematic property name</param>
        /// <returns>The safe alternative property name, or null if no alternative exists</returns>
        public static string GetSafeAlternative(Type componentType, string problematicPropertyName)
        {
            if (componentType == null || string.IsNullOrEmpty(problematicPropertyName))
                return null;

            // Renderer alternatives
            if (typeof(Renderer).IsAssignableFrom(componentType))
            {
                switch (problematicPropertyName)
                {
                    case "material":
                        return "sharedMaterial";
                    case "materials":
                        return "sharedMaterials";
                }
            }

            // MeshFilter alternatives
            if (typeof(MeshFilter).IsAssignableFrom(componentType))
            {
                if (problematicPropertyName == "mesh")
                    return "sharedMesh";
            }

            // No safe alternative found
            return null;
        }

        /// <summary>
        /// Gets all safe alternative property names that should be included for a component type.
        /// This includes safe versions of commonly used properties that might not be automatically discovered.
        /// </summary>
        /// <param name="componentType">The component type to get safe alternatives for</param>
        /// <returns>Array of safe property names to include</returns>
        public static string[] GetSafeAlternatives(Type componentType)
        {
            if (componentType == null)
                return new string[0];

            var alternatives = new System.Collections.Generic.List<string>();

            // Add safe alternatives for Renderer components
            if (typeof(Renderer).IsAssignableFrom(componentType))
            {
                alternatives.Add("sharedMaterial");
                alternatives.Add("sharedMaterials");
            }

            // Add safe alternatives for MeshFilter components
            if (typeof(MeshFilter).IsAssignableFrom(componentType))
            {
                alternatives.Add("sharedMesh");
            }

            return alternatives.ToArray();
        }

        /// <summary>
        /// Validates a property configuration and returns a user-friendly message if there are issues.
        /// </summary>
        /// <param name="componentType">The component type containing the property</param>
        /// <param name="propertyName">The property name to validate</param>
        /// <returns>Null if the property is safe, otherwise a warning message</returns>
        public static string ValidateProperty(Type componentType, string propertyName)
        {
            if (!IsProblematicProperty(componentType, propertyName))
                return null;

            string safeAlternative = GetSafeAlternative(componentType, propertyName);
            
            if (!string.IsNullOrEmpty(safeAlternative))
            {
                return $"⚠️ Property '{propertyName}' creates instances in edit mode. Consider using '{safeAlternative}' instead.";
            }
            else
            {
                return $"⚠️ Property '{propertyName}' creates instances in edit mode and should be avoided.";
            }
        }
    }
}
