using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace System
{
    /// <summary>
    /// An attribute which attaches a description and a display name to a member
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.All)]
    public class DescriptionAttribute : Attribute
    {
        private string displayName = null;

        /// <summary>
        /// The name used to display this member (in the GUI, for example). Defaults to the name of the member
        /// </summary>
        public string DisplayName
        {
            get { return this.displayName ?? (this.Member != null ? this.Member.Name : null); }
            set { this.displayName = value; }
        }

        private readonly string description;

        /// <summary>
        /// A description of this member (could be used as a GUI tooltip)
        /// </summary>
        public string Description { get { return this.description; } }

        /// <summary>
        /// This field represents the member which this attribute targets.
        /// It is set when the attribute is loaded.
        /// </summary>
        public MemberInfo Member { get; protected set; }

        /// <summary>
        /// Create a description attribute from a description
        /// </summary>
        public DescriptionAttribute(string description)
        {
            this.description = description;
            this.Member = null;
        }

        /// <summary>
        /// Loads a description for the member
        /// </summary>
        public static DescriptionAttribute LoadDescription(MemberInfo member)
        {
            var attribute = member.GetCustomAttributes(typeof(DescriptionAttribute), true).FirstOrDefault() as DescriptionAttribute;
            if (attribute != null)
                attribute.Member = member;

            return attribute;
        }
    }

    /// <summary>
    /// An attribute which provides metadata about a property
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Property)]
    public class ParameterAttribute : DescriptionAttribute
    {
        /// <summary>
        /// The default value for the parameter. This can either be an object
        /// of the parameter's type or the Type object representing the parameter's type.
        /// In the second case, setting parameter defaults attempts to instantiate a new object
        /// of the provided type using a default constructor.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// The minimum value which the parameter can take
        /// </summary>
        public object MinValue { get; set; }

        /// <summary>
        /// The maximum value which the parameter can take
        /// </summary>
        public object MaxValue { get; set; }

        /// <summary>
        /// A PropertyInfo view of the Member property.
        /// </summary>
        public PropertyInfo Property
        {
            get { return (PropertyInfo)this.Member; }
            private set { this.Member = value; }
        }

        /// <summary>
        /// Construct a parameter attribute with the given description
        /// </summary>
        public ParameterAttribute(string description)
            : base(description)
        {
            this.DefaultValue = this.MinValue = this.MaxValue = null;
        }

        /// <summary>
        /// Loads the parameter for the given property
        /// </summary>
        public static ParameterAttribute LoadParameter(PropertyInfo property)
        {
            var attribute = property.GetCustomAttributes(typeof(DescriptionAttribute), true).FirstOrDefault() as ParameterAttribute;
            if (attribute != null)
                attribute.Property = property;

            return attribute;
        }
    }

    /// <summary>
    /// Provides utilities and extension methods for parameters and descriptions
    /// </summary>
    public static class Parameters
    {
        /// <summary>
        /// Does the parameter have a non-null default value?
        /// </summary>
        public static bool HasDefaultValue(this ParameterAttribute attr)
        {
            return attr.DefaultValue != null;
        }

        /// <summary>
        /// Does the parameter hava a non-null minimum value?
        /// </summary>
        public static bool HasMinValue(this ParameterAttribute attr)
        {
            return attr.MinValue != null;
        }

        /// <summary>
        /// Does the parameter have a non-null maximum value?
        /// </summary>
        public static bool HasMaxValue(this ParameterAttribute attr)
        {
            return attr.MaxValue != null;
        }

        /// <summary>
        /// Adjusts the given value to conform to the parameters specified maximums and minimums
        /// </summary>
        public static IComparable GetClosestValueInBounds(this ParameterAttribute attr, IComparable value)
        {
            if (attr.HasMinValue() && value.CompareTo(attr.MinValue) <= 0)
                return (IComparable)attr.MinValue;
            if (attr.HasMaxValue() && value.CompareTo(attr.MaxValue) >= 0)
                return (IComparable)attr.MaxValue;
            return value;
        }

        /// <summary>
        /// Get the parameters defined by the specified type
        /// </summary>
        public static IEnumerable<ParameterAttribute> GetParametersForType(this Type type)
        {
            return type.GetProperties()
                .Select(ParameterAttribute.LoadParameter)
                .Where(a => a != null);
        }

        /// <summary>
        /// Get the parameters defined by the object's type
        /// </summary>
        public static IEnumerable<ParameterAttribute> GetParameters(this object obj)
        {
            return obj.GetType().GetParametersForType();
        }

        /// <summary>
        /// Gets the description attribute for the specified type
        /// </summary>
        public static DescriptionAttribute GetDescriptionForType(this Type type)
        {
            return DescriptionAttribute.LoadDescription(type);
        }

        /// <summary>
        /// Gets the description attribute for the object's type
        /// </summary>
        public static DescriptionAttribute GetDescription(this object obj)
        {
            return obj.GetType().GetDescriptionForType();
        }

        /// <summary>
        /// Gets the description attribute for the specified enumeration value
        /// </summary>
        public static DescriptionAttribute GetDescriptionForEnum(this Enum enumeration)
        {
            return DescriptionAttribute.LoadDescription(enumeration.GetType().GetMember(enumeration.ToString())[0]);
        }

        /// <summary>
        /// Sets the object's parameters to their specified default values. This should be called
        /// in the constructor of most classes which define parameters.
        /// </summary>
        public static void SetParametersToDefaultValues(this object obj)
        {
            foreach (var param in obj.GetParameters())
                if (param.HasDefaultValue())
                {
                    // if the parameter is not a Type but its default value is, setting the parameter to its default value
                    // sets it to a new instance of that type
                    if (param.DefaultValue is Type && param.Property.PropertyType != typeof(Type))
                        obj.SetProperty(param.Property, ((Type)param.DefaultValue).GetConstructor(new Type[0]).Invoke(Utils.EmptyArgs));
                    else
                        obj.SetProperty(param.Property, param.DefaultValue);
                }
        }

        /// <summary>
        /// Are the object's parameters set to valid values?
        /// </summary>
        public static bool AreParameterValuesValid(this object obj, out string errorMessage)
        {
            object value;
            foreach (var param in obj.GetParameters())
            {
                value = obj.GetProperty(param.Property);
                if (param.HasMinValue() && ((IComparable)value).CompareTo(param.MinValue) < 0)
                {
                    errorMessage = param.DisplayName + "cannot have a value < " + param.MinValue + " (current value = " + value + ")";
                    return false;
                }

                if (param.HasMaxValue() && ((IComparable)value).CompareTo(param.MaxValue) > 0)
                {
                    errorMessage = param.DisplayName + "cannot have a value > " + param.MaxValue + " (current value = " + value + ")";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Transfers the parameter settings from one object to another of the same type
        /// </summary>
        public static void TransferParameterValuesTo<T>(this T from, T to)
        {
            foreach (var param in from.GetParameters())
                to.SetProperty(param.Property, from.GetProperty(param.Property));
        }

        /// <summary>
        /// Sets an object's value for property to value
        /// </summary>
        public static void SetProperty(this object obj, PropertyInfo property, object value)
        {
            property.SetValue(obj, value, null);
        }

        /// <summary>
        /// Gets the value stored in an object's property
        /// </summary>
        public static object GetProperty(this object obj, PropertyInfo property)
        {
            return property.GetValue(obj, null);
        }

        /// <summary>
        /// Returns a factory function which can produce new objects with the same parameter values as obj.
        /// The object must have a parameterless constructor for this to work.
        /// </summary>
        public static Func<T> GetFactory<T>(this T obj)
        {
            var parameters = obj
                .GetParameters()
                .Select(p => new { Property = p.Property, Value = obj.GetProperty(p.Property) })
                .ToIArray();

            return () =>
            {
                var newObj = (T)obj.GetType().New();
                foreach (var param in parameters)
                    newObj.SetProperty(param.Property, param.Value);

                return newObj;
            };
        }

        /// <summary>
        /// Returns true if that has equal parameter property values to this.
        /// </summary>
        public static bool HasEqualParameters<T>(this T obj, T that)
        {
            object thisVal, thatVal;
            foreach (var param in obj.GetParameters())
            {
                thisVal = obj.GetProperty(param.Property);
                thatVal = that.GetProperty(param.Property);
                if (thisVal == null ? thisVal != thatVal : !thisVal.Equals(thatVal))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the member's display name or else its member name if its display
        /// name is null
        /// </summary>
        public static string DisplayName(this MemberInfo member)
        {
            var descriptions = member.GetCustomAttributes(typeof(DescriptionAttribute), true);

            return descriptions.Length == 0
                ? member.Name
                : ((DescriptionAttribute)descriptions[0]).DisplayName ?? member.Name;
        }

        /// <summary>
        /// Returns the member's description
        /// </summary>
        public static string Description(this MemberInfo member)
        {
            return ((DescriptionAttribute)member.GetCustomAttributes(typeof(DescriptionAttribute), true).First()).Description;
        }

        /// <summary>
        /// Returns a string that could be used for pretty-printing the object
        /// </summary>
        public static string PrettyPrint(this object obj, bool printHeader = false)
        {
            var sb = new StringBuilder();
            obj.PrettyPrintRecursive(sb, printHeader ? 1 : 0, printHeader);
            string result = sb.ToString();

            return result.EndsWith(Environment.NewLine)
                ? result.Substring(0, result.Length - Environment.NewLine.Length)
                : result;
        }

        private static void PrettyPrintRecursive(this object obj, StringBuilder sb, int indentation, bool printHeader)
        {
            DescriptionAttribute description;
            if (obj.GetParameters().IsEmpty())
            {
                description = obj.GetType().IsEnum ? ((Enum)obj).GetDescriptionForEnum() : null;
                string toString = obj.ToString();
                if (description == null && toString == obj.GetType().FullName)
                    description = obj.GetDescription();
                sb.Append('=').AppendLine(description == null ? obj.ToString() : description.DisplayName);
                return;
            }

            if (printHeader)
            {
                description = obj.GetDescription();
                sb.Append('=').Append(description == null ? obj.GetType().Name : description.DisplayName);
            }
            if (indentation > 0)
                sb.AppendLine(" {");
            foreach (var param in obj.GetParameters())
            {
                sb.AppendTabs(indentation).Append(param.DisplayName);
                var value = obj.GetProperty(param.Property);
                if (value == null)
                    sb.AppendLine("null");
                else
                    value.PrettyPrintRecursive(sb, indentation + 1, value.GetType() != param.Property.PropertyType);
            }
            if (indentation > 0)
                sb.AppendTabs(Math.Max(0, indentation - 1)).AppendLine("}");
        }

        private static StringBuilder AppendTabs(this StringBuilder sb, int tabCount)
        {
            for (int i = 0; i < tabCount; i++)
                sb.Append('\t');
            return sb;
        }
    }
}
