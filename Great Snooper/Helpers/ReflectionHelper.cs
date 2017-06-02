using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GreatSnooper.Helpers
{
    static class ReflectionHelper
    {
        public static List<Type> GetTypesThatInheritsFrom(Type type)
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t != type && type.IsAssignableFrom(t))
                .ToList();
        }
    }
}
