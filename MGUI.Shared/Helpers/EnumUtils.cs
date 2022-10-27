using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public static class EnumUtils
    {
        //Taken from: https://stackoverflow.com/questions/45426266/get-description-attributes-from-a-flagged-enum
        /// <summary>Returns the description attribute of this Enum value</summary>
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                System.Reflection.FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }

        /// <summary>Returns the Name property value of the DataMember attribute applied to the given enum value, or null if the enum value does not have a DataMember attribute applied to it.</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDataMemberName(this Enum value)
        {
            DataMemberAttribute nAttribute = value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DataMemberAttribute), false).SingleOrDefault() as DataMemberAttribute;
            return nAttribute?.Name;
            //return GetAttribute<DataMemberAttribute>(value).Name;
        }

        public static T GetAttribute<T>(this Enum value) where T : Attribute
        {
            return value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(T), false).SingleOrDefault() as T;
        }

        /// <summary>Attempts to parse the given string <paramref name="value"/> to the given <typeparamref name="TEnum"/>, 
        /// comparing against the 'DataMember' attribute name of each enum value, rather than using .ToString() on the enum value.<para/>
        /// Returns null if no match is found.</summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TEnum? ParseEnumOrNullFromDataMemberName<TEnum>(string value)
            where TEnum : struct, Enum
        {
            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
            {
                if (enumValue.GetDataMemberName() == value)
                {
                    return enumValue;
                }
            }

            return null;
        }
    }
}
