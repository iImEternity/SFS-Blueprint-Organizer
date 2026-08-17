using System;
using System.Reflection;

namespace SFSBlueprintOrganizer
{
    public static class SafeAccess
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static object GetRaw(object obj, string name)
        {
            if (obj == null) return null;
            Type type = obj.GetType();

            while (type != null)
            {
                FieldInfo f = type.GetField(name, Flags);
                if (f != null) return f.GetValue(obj);

                PropertyInfo p = type.GetProperty(name, Flags);
                if (p != null) return p.GetValue(obj, null);

                type = type.BaseType;
            }
            return null;
        }

        public static T Get<T>(object obj, string name, T fallback = default)
        {
            try
            {
                object raw = GetRaw(obj, name);
                if (raw == null) return fallback;

                if (!(raw is T))
                {
                    object inner = GetRaw(raw, "Value");
                    if (inner is T innerVal) return innerVal;
                    if (inner != null)
                    {
                        try { return (T)Convert.ChangeType(inner, typeof(T)); } catch { }
                    }
                    try { return (T)Convert.ChangeType(raw, typeof(T)); } catch { }
                    return fallback;
                }

                return (T)raw;
            }
            catch
            {
                return fallback;
            }
        }

        public static T GetValue<T>(object wrapperObj, T fallback = default)
        {
            try
            {
                if (wrapperObj == null) return fallback;
                object v = GetRaw(wrapperObj, "Value");
                if (v is T val) return val;
                if (v != null) return (T)Convert.ChangeType(v, typeof(T));
                return fallback;
            }
            catch
            {
                return fallback;
            }
        }

        public static double GetNumeric(object obj, string name, double fallback = 0)
        {
            object raw = GetRaw(obj, name);
            if (raw == null) return fallback;

            try
            {
                return Convert.ToDouble(raw);
            }
            catch
            {
                object inner = GetRaw(raw, "Value");
                if (inner != null)
                {
                    try { return Convert.ToDouble(inner); } catch { }
                }
                return fallback;
            }
        }

        public static bool Set<T>(object obj, string name, T value)
        {
            if (obj == null) return false;
            Type type = obj.GetType();

            while (type != null)
            {
                try
                {
                    FieldInfo f = type.GetField(name, Flags);
                    if (f != null) { f.SetValue(obj, value); return true; }

                    PropertyInfo p = type.GetProperty(name, Flags);
                    if (p != null && p.CanWrite) { p.SetValue(obj, value, null); return true; }
                }
                catch
                {
                    return false;
                }
                type = type.BaseType;
            }
            return false;
        }

        public static object CallStatic(string typeFullName, string methodOrPropName)
        {
            try
            {
                Type t = FindType(typeFullName);
                if (t == null) return null;

                PropertyInfo p = t.GetProperty(methodOrPropName, BindingFlags.Public | BindingFlags.Static);
                if (p != null) return p.GetValue(null, null);

                FieldInfo f = t.GetField(methodOrPropName, BindingFlags.Public | BindingFlags.Static);
                if (f != null) return f.GetValue(null);

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }
    }
}
