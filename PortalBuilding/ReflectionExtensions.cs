using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

internal static class ReflectionExtensions
{
    public static MethodInfo GetInstancePrivateMethod(this Type t, string name)
    {
        return t.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
