using System;
using System.Reflection;
using System.Text;

namespace Kermalis.EndianBinaryIO
{
    internal sealed class Utils
    {
        public static void TruncateString(string str, int charCount, out char[] toArray)
        {
            toArray = new char[charCount];
            int numCharsToCopy = Math.Min(charCount, str.Length);
            for (int i = 0; i < numCharsToCopy; i++)
            {
                toArray[i] = str[i];
            }
        }

        public static bool TryGetAttribute<TAttribute>(PropertyInfo propertyInfo, out TAttribute attribute) where TAttribute : Attribute
        {
            object[] attributes = propertyInfo.GetCustomAttributes(typeof(TAttribute), true);
            if (attributes.Length == 1)
            {
                attribute = (TAttribute)attributes[0];
                return true;
            }
            attribute = null;
            return false;
        }
        public static TValue GetAttributeValue<TAttribute, TValue>(TAttribute attribute) where TAttribute : Attribute, IBinaryAttribute<TValue>
        {
            return (TValue)typeof(TAttribute).GetProperty(nameof(IBinaryAttribute<TValue>.Value)).GetValue(attribute);
        }
        public static TValue AttributeValueOrDefault<TAttribute, TValue>(PropertyInfo propertyInfo, TValue defaultValue) where TAttribute : Attribute, IBinaryAttribute<TValue>
        {
            if (TryGetAttribute(propertyInfo, out TAttribute attribute))
            {
                return GetAttributeValue<TAttribute, TValue>(attribute);
            }
            return defaultValue;
        }

        public static void ThrowIfCannotReadWriteType(Type type)
        {
            if (type.IsArray || type.IsEnum || type.IsInterface || type.IsPointer || type.IsPrimitive)
            {
                throw new ArgumentException(nameof(type), $"Cannot read/write \"{type.FullName}\" objects.");
            }
        }
        public static void ThrowIfCannotUseEncoding(Encoding encoding)
        {
            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding), "EndianBinaryIO could not read/write chars because an encoding was null; make sure you pass one in properly.");
            }
        }

        // Returns true if count is 0
        public static bool ValidateReadArraySize<T>(int count, out T[] array)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException($"Invalid array length ({count})");
            }
            if (count == 0)
            {
                array = Array.Empty<T>();
                return true;
            }
            array = null;
            return false;
        }
        // Returns true if count is 0
        public static bool ValidateArrayIndexAndCount(Array array, int startIndex, int count)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException($"Invalid array length ({count})");
            }
            if (startIndex + count > array.Length)
            {
                throw new ArgumentOutOfRangeException($"Invalid array index + count (StartIndex: {startIndex}, Count: {count}, Array Length: {array.Length})");
            }
            return count == 0;
        }

        public static int GetArrayLength(object obj, Type objType, PropertyInfo propertyInfo)
        {
            int Validate(int value)
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException($"An array property in \"{objType.FullName}\" has an invalid length attribute ({value})");
                }
                return value;
            }

            if (TryGetAttribute(propertyInfo, out BinaryArrayFixedLengthAttribute fixedLenAttribute))
            {
                if (propertyInfo.IsDefined(typeof(BinaryArrayVariableLengthAttribute)))
                {
                    throw new ArgumentException($"An array property in \"{objType.FullName}\" has two array length attributes. Only one should be provided.");
                }
                return Validate(GetAttributeValue<BinaryArrayFixedLengthAttribute, int>(fixedLenAttribute));
            }
            if (TryGetAttribute(propertyInfo, out BinaryArrayVariableLengthAttribute varLenAttribute))
            {
                string anchorName = GetAttributeValue<BinaryArrayVariableLengthAttribute, string>(varLenAttribute);
                PropertyInfo anchor = objType.GetProperty(anchorName, BindingFlags.Instance | BindingFlags.Public);
                if (anchor is null)
                {
                    throw new MissingMemberException($"An array property in \"{objType.FullName}\" has an invalid {nameof(BinaryArrayVariableLengthAttribute)} ({anchorName}).");
                }
                return Validate(Convert.ToInt32(anchor.GetValue(obj)));
            }
            throw new MissingMemberException($"An array property in \"{objType.FullName}\" is missing an array length attribute. One should be provided.");
        }
        public static void GetStringLength(object obj, Type objType, PropertyInfo propertyInfo, bool forReads, out bool? nullTerminated, out int stringLength)
        {
            int Validate(int value)
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException($"A string property in \"{objType.FullName}\" has an invalid length attribute ({value})");
                }
                return value;
            }

            if (TryGetAttribute(propertyInfo, out BinaryStringNullTerminatedAttribute nullTermAttribute))
            {
                if (propertyInfo.IsDefined(typeof(BinaryStringFixedLengthAttribute)) || propertyInfo.IsDefined(typeof(BinaryStringVariableLengthAttribute)))
                {
                    throw new ArgumentException($"A string property in \"{objType.FullName}\" has a string length attribute and a {nameof(BinaryStringNullTerminatedAttribute)}; cannot use both.");
                }
                if (propertyInfo.IsDefined(typeof(BinaryStringTrimNullTerminatorsAttribute)))
                {
                    throw new ArgumentException($"A string property in \"{objType.FullName}\" has a {nameof(BinaryStringNullTerminatedAttribute)} and a {nameof(BinaryStringTrimNullTerminatorsAttribute)}; cannot use both.");
                }
                bool nt = GetAttributeValue<BinaryStringNullTerminatedAttribute, bool>(nullTermAttribute);
                if (forReads && !nt) // Not forcing BinaryStringNullTerminatedAttribute to be treated as true since you may only write objects without reading them.
                {
                    throw new ArgumentException($"A string property in \"{objType.FullName}\" has a {nameof(BinaryStringNullTerminatedAttribute)} that's set to false." +
                        $" Must use null termination or provide a string length when reading.");
                }
                nullTerminated = nt;
                stringLength = -1;
                return;
            }
            if (TryGetAttribute(propertyInfo, out BinaryStringFixedLengthAttribute fixedLenAttribute))
            {
                if (propertyInfo.IsDefined(typeof(BinaryStringVariableLengthAttribute)))
                {
                    throw new ArgumentException($"A string property in \"{objType.FullName}\" has two string length attributes. Only one should be provided.");
                }
                nullTerminated = null;
                stringLength = Validate(GetAttributeValue<BinaryStringFixedLengthAttribute, int>(fixedLenAttribute));
                return;
            }
            if (TryGetAttribute(propertyInfo, out BinaryStringVariableLengthAttribute varLenAttribute))
            {
                string anchorName = GetAttributeValue<BinaryStringVariableLengthAttribute, string>(varLenAttribute);
                PropertyInfo anchor = objType.GetProperty(anchorName, BindingFlags.Instance | BindingFlags.Public);
                if (anchor is null)
                {
                    throw new MissingMemberException($"A string property in \"{objType.FullName}\" has an invalid {nameof(BinaryStringVariableLengthAttribute)} ({anchorName}).");
                }
                nullTerminated = null;
                stringLength = Validate(Convert.ToInt32(anchor.GetValue(obj)));
                return;
            }
            throw new MissingMemberException($"A string property in \"{objType.FullName}\" is missing a string length attribute and has no {nameof(BinaryStringNullTerminatedAttribute)}. One should be provided.");
        }
    }
}
