using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET
{
    public sealed class BTNodeFieldMeta
    {
        public string Name;

        public Type FieldType;

        public bool IsInput;

        public bool IsOutput;

        public Type IntDropdownType;
    }

    public static class BTNodeMeta
    {
        [StaticField]
        private static Dictionary<string, Type> fallbackTypes;

        public static Dictionary<string, Type> GetTypes()
        {
            if (CodeTypes.Instance != null)
            {
                Dictionary<string, Type> types = CodeTypes.Instance.GetTypes();
                if (types != null && types.Count > 0)
                {
                    return types;
                }
            }

            if (fallbackTypes != null)
            {
                return fallbackTypes;
            }

            List<Assembly> assemblies = new();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                assemblies.Add(assembly);
            }

            Dictionary<string, Type> scanned = new();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        scanned[type.FullName] = type;
                    }
                }
                catch (Exception)
                {
                }
            }

            fallbackTypes = scanned;
            return fallbackTypes;
        }

        public static List<BTNodeFieldMeta> GetFields(Type nodeType)
        {
            List<BTNodeFieldMeta> result = new();
            foreach (FieldInfo field in nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                result.Add(new BTNodeFieldMeta
                {
                    Name = field.Name,
                    FieldType = field.FieldType,
                    IsInput = field.IsDefined(typeof (BTInput), true),
                    IsOutput = field.IsDefined(typeof (BTOutput), true),
                    IntDropdownType = field.GetCustomAttribute<BTIntDropdownAttribute>()?.EnumSingletonType,
                });
            }

            return result;
        }

        public static float[] GetTitleColor(Type nodeType)
        {
            BTNodeTitleColorAttribute attribute = nodeType.GetCustomAttribute<BTNodeTitleColorAttribute>(true);
            if (attribute == null)
            {
                return null;
            }

            return new[] { attribute.R, attribute.G, attribute.B, attribute.A };
        }

        public static List<Type> GetNodeTypes()
        {
            List<Type> result = new();
            foreach (Type type in GetTypes().Values)
            {
                if (!typeof (BTNode).IsAssignableFrom(type) || type.IsAbstract)
                {
                    continue;
                }

                result.Add(type);
            }

            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }
    }
}
