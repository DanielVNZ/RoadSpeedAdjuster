using System.Reflection;
using Colossal.Logging;

namespace RoadSpeedAdjuster.Extensions
{
    /// <summary>
    /// Extension methods to make reflection easier.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Quick way to use all relevant binding flags.
        /// </summary>
        public static readonly BindingFlags AllFlags =
            BindingFlags.DeclaredOnly |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.GetField |
            BindingFlags.GetProperty;

        private static ILog Log => Mod.log ?? LogManager.GetLogger("RoadSpeedAdjuster.Reflection");

        /// <summary>
        /// Uses reflection to get the value of a member of an object.
        /// </summary>
        public static object GetMemberValue(this object obj, string memberName)
        {
            var memInf = GetMemberInfo(obj, memberName);
            if (memInf == null)
            {
                Log.Error($"[ReflectionExtensions] Could not find member '{memberName}' on type {obj.GetType().FullName}");
                return null;
            }

            return memInf switch
            {
                PropertyInfo pi => pi.GetValue(obj, null),
                FieldInfo fi => fi.GetValue(obj),
                _ => null
            };
        }

        /// <summary>
        /// Uses Reflection to set the value of a member of an object.
        /// </summary>
        public static object SetMemberValue(this object obj, string memberName, object newValue)
        {
            var memInf = GetMemberInfo(obj, memberName);
            if (memInf == null)
            {
                Log.Error($"[ReflectionExtensions] Could not find member '{memberName}' on type {obj.GetType().FullName}");
                return null;
            }

            var oldValue = obj.GetMemberValue(memberName);

            switch (memInf)
            {
                case PropertyInfo pi:
                    pi.SetValue(obj, newValue, null);
                    break;

                case FieldInfo fi:
                    fi.SetValue(obj, newValue);
                    break;
            }

            return oldValue;
        }

        /// <summary>
        /// Uses reflection to get member info.
        /// </summary>
        private static MemberInfo GetMemberInfo(object obj, string memberName)
        {
            // Property first
            var prop = obj.GetType().GetProperty(
                memberName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (prop != null)
                return prop;

            // Then field
            var field = obj.GetType().GetField(
                memberName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            return field;
        }

        [System.Diagnostics.DebuggerHidden]
        private static T As<T>(this object obj)
        {
            return (T)obj;
        }
    }
}
