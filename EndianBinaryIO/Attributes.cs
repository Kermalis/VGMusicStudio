using System;
using System.Text;

namespace Kermalis.EndianBinaryIO
{
    public interface IBinaryAttribute<T>
    {
        T Value { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryIgnoreAttribute : Attribute, IBinaryAttribute<bool>
    {
        public bool Value { get; }

        public BinaryIgnoreAttribute(bool ignore = true)
        {
            Value = ignore;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryBooleanSizeAttribute : Attribute, IBinaryAttribute<BooleanSize>
    {
        public BooleanSize Value { get; }

        public BinaryBooleanSizeAttribute(BooleanSize booleanSize)
        {
            if (booleanSize >= BooleanSize.MAX)
            {
                throw new ArgumentOutOfRangeException($"{nameof(BinaryBooleanSizeAttribute)} cannot be created with a size of {booleanSize}.");
            }
            Value = booleanSize;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryEncodingAttribute : Attribute, IBinaryAttribute<Encoding>
    {
        public Encoding Value { get; }

        public BinaryEncodingAttribute(string encodingName)
        {
            Value = Encoding.GetEncoding(encodingName);
        }
        public BinaryEncodingAttribute(int encodingCodepage)
        {
            Value = Encoding.GetEncoding(encodingCodepage);
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryStringNullTerminatedAttribute : Attribute, IBinaryAttribute<bool>
    {
        public bool Value { get; }

        public BinaryStringNullTerminatedAttribute(bool nullTerminated = true)
        {
            Value = nullTerminated;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryArrayFixedLengthAttribute : Attribute, IBinaryAttribute<int>
    {
        public int Value { get; }

        public BinaryArrayFixedLengthAttribute(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(BinaryArrayFixedLengthAttribute)} cannot be created with a length of {length}. Length must be 0 or greater.");
            }
            Value = length;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryArrayVariableLengthAttribute : Attribute, IBinaryAttribute<string>
    {
        public string Value { get; }

        public BinaryArrayVariableLengthAttribute(string anchor)
        {
            Value = anchor;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryStringFixedLengthAttribute : Attribute, IBinaryAttribute<int>
    {
        public int Value { get; }

        public BinaryStringFixedLengthAttribute(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(BinaryStringFixedLengthAttribute)} cannot be created with a length of {length}. Length must be 0 or greater.");
            }
            Value = length;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryStringVariableLengthAttribute : Attribute, IBinaryAttribute<string>
    {
        public string Value { get; }

        public BinaryStringVariableLengthAttribute(string anchor)
        {
            Value = anchor;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinaryStringTrimNullTerminatorsAttribute : Attribute, IBinaryAttribute<bool>
    {
        public bool Value { get; }

        public BinaryStringTrimNullTerminatorsAttribute(bool trim = true)
        {
            Value = trim;
        }
    }
}
