//******************************************************************************************************
//  Int24.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  11/12/2004 - J. Ritchie Carroll
//       Initial version of source generated.
//  08/3/2009 - Josh L. Patterson
//       Updated comments.
//  08/11/2009 - Josh L. Patterson
//       Updated comments.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

#region [ Contributor License Agreements ]

/**************************************************************************\
   Copyright © 2009 - J. Ritchie Carroll
   All rights reserved.
  
   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:
  
      * Redistributions of source code must retain the above copyright
        notice, this list of conditions and the following disclaimer.
       
      * Redistributions in binary form must reproduce the above
        copyright notice, this list of conditions and the following
        disclaimer in the documentation and/or other materials provided
        with the distribution.
  
   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER "AS IS" AND ANY
   EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
   PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
   OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
   OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
  
\**************************************************************************/

#endregion

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System;

/// <summary>Represents a 3-byte, 24-bit signed integer.</summary>
/// <remarks>
/// <para>
/// This class behaves like most other intrinsic signed integers but allows a 3-byte, 24-bit integer implementation
/// that is often found in many digital-signal processing arenas and different kinds of protocol parsing.  A signed
/// 24-bit integer is typically used to save storage space on disk where its value range of -8388608 to 8388607 is
/// sufficient, but the signed Int16 value range of -32768 to 32767 is too small.
/// </para>
/// <para>
/// This structure uses an Int32 internally for storage and most other common expected integer functionality, so using
/// a 24-bit integer will not save memory.  However, if the 24-bit signed integer range (-8388608 to 8388607) suits your
/// data needs you can save disk space by only storing the three bytes that this integer actually consumes.  You can do
/// this by calling the Int24.GetBytes function to return a three byte binary array that can be serialized to the desired
/// destination and then calling the Int24.GetValue function to restore the Int24 value from those three bytes.
/// </para>
/// <para>
/// All the standard operators for the Int24 have been fully defined for use with both Int24 and Int32 signed integers;
/// you should find that without the exception Int24 can be compared and numerically calculated with an Int24 or Int32.
/// Necessary casting should be minimal and typical use should be very simple - just as if you are using any other native
/// signed integer.
/// </para>
/// </remarks>
[Serializable]
public struct Int24 : IComparable, IFormattable, IConvertible, IComparable<Int24>, IComparable<int>, IEquatable<Int24>, IEquatable<int>
{
    #region [ Members ]

    // Constants
    private const int MaxValue32 = 8388607; // Represents the largest possible value of an Int24 as an Int32.
    private const int MinValue32 = -8388608; // Represents the smallest possible value of an Int24 as an Int32.

    /// <summary>High byte bit-mask used when a 24-bit integer is stored within a 32-bit integer. This field is constant.</summary>
    public const int BitMask = -16777216;

    // Fields
    private readonly int m_value; // We internally store the Int24 value in a 4-byte integer for convenience

    #endregion

    #region [ Constructors ]

    /// <summary>Creates 24-bit signed integer from an existing 24-bit signed integer.</summary>
    /// <param name="value">24-but signed integer to create new Int24 from.</param>
    public Int24(Int24 value)
    {
        m_value = ApplyBitMask(value);
    }

    /// <summary>Creates 24-bit signed integer from a 32-bit signed integer.</summary>
    /// <param name="value">32-bit signed integer to use as new 24-bit signed integer value.</param>
    /// <exception cref="OverflowException">Source values outside 24-bit min/max range will cause an overflow exception.</exception>
    public Int24(int value)
    {
        ValidateNumericRange(value);
        m_value = ApplyBitMask(value);
    }

    /// <summary>Creates 24-bit signed integer from three bytes at a specified position in a byte array.</summary>
    /// <param name="value">An array of bytes.</param>
    /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
    /// <remarks>
    /// <para>You can use this constructor in-lieu of a System.BitConverter.ToInt24 function.</para>
    /// <para>Bytes endian order assumed to match that of currently executing process architecture (little-endian on Intel platforms).</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is greater than <paramref name="value"/> length.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> length from <paramref name="startIndex"/> is too small to represent a <see cref="UInt24"/>.</exception>
    public Int24(byte[] value, int startIndex)
    {
        m_value = GetValue(value, startIndex).m_value;
    }

    #endregion

    #region [ Methods ]

    /// <summary>Returns the Int24 value as an array of three bytes.</summary>
    /// <returns>An array of bytes with length 3.</returns>
    /// <remarks>
    /// <para>You can use this function in-lieu of a System.BitConverter.GetBytes function.</para>
    /// <para>Bytes will be returned in endian order of currently executing process architecture (little-endian on Intel platforms).</para>
    /// </remarks>
    public byte[] GetBytes()
    {
        // Return serialized 3-byte representation of Int24
        return GetBytes(this);
    }

    /// <summary>
    /// Compares this instance to a specified object and returns an indication of their relative values.
    /// </summary>
    /// <param name="value">An object to compare, or null.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and value. Returns less than zero
    /// if this instance is less than value, zero if this instance is equal to value, or greater than zero
    /// if this instance is greater than value.
    /// </returns>
    /// <exception cref="ArgumentException">value is not an Int32 or Int24.</exception>
    public int CompareTo(object? value)
    {
        if (value is null)
            return 1;

        if (value is not int && value is not Int24)
            throw new ArgumentException("Argument must be an Int32 or an Int24");

        int num = (int)value;

        return m_value < num ? -1 : m_value > num ? 1 : 0;
    }

    /// <summary>
    /// Compares this instance to a specified 24-bit signed integer and returns an indication of their
    /// relative values.
    /// </summary>
    /// <param name="value">An integer to compare.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and value. Returns less than zero
    /// if this instance is less than value, zero if this instance is equal to value, or greater than zero
    /// if this instance is greater than value.
    /// </returns>
    public int CompareTo(Int24 value)
    {
        return CompareTo((int)value);
    }

    /// <summary>
    /// Compares this instance to a specified 32-bit signed integer and returns an indication of their
    /// relative values.
    /// </summary>
    /// <param name="value">An integer to compare.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and value. Returns less than zero
    /// if this instance is less than value, zero if this instance is equal to value, or greater than zero
    /// if this instance is greater than value.
    /// </returns>
    public int CompareTo(int value)
    {
        return m_value < value ? -1 : m_value > value ? 1 : 0;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="obj">An object to compare, or null.</param>
    /// <returns>
    /// True if obj is an instance of Int32 or Int24 and equals the value of this instance;
    /// otherwise, False.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is int or Int24)
            return Equals((int)obj);

        return false;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified Int24 value.
    /// </summary>
    /// <param name="obj">An Int24 value to compare to this instance.</param>
    /// <returns>
    /// True if obj has the same value as this instance; otherwise, False.
    /// </returns>
    public bool Equals(Int24 obj)
    {
        return Equals((int)obj);
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified Int32 value.
    /// </summary>
    /// <param name="obj">An Int32 value to compare to this instance.</param>
    /// <returns>
    /// True if obj has the same value as this instance; otherwise, False.
    /// </returns>
    public bool Equals(int obj)
    {
        return m_value == obj;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer hash code.
    /// </returns>
    public override int GetHashCode()
    {
        return m_value;
    }

    /// <summary>
    /// Converts the numeric value of this instance to its equivalent string representation.
    /// </summary>
    /// <returns>
    /// The string representation of the value of this instance, consisting of a minus sign if
    /// the value is negative, and a sequence of digits ranging from 0 to 9 with no leading zeroes.
    /// </returns>
    public override string ToString()
    {
        return m_value.ToString();
    }

    /// <summary>
    /// Converts the numeric value of this instance to its equivalent string representation, using
    /// the specified format.
    /// </summary>
    /// <param name="format">A format string.</param>
    /// <returns>
    /// The string representation of the value of this instance as specified by format.
    /// </returns>
    public string ToString(string? format)
    {
        return m_value.ToString(format);
    }

    /// <summary>
    /// Converts the numeric value of this instance to its equivalent string representation using the
    /// specified culture-specific format information.
    /// </summary>
    /// <param name="provider">
    /// A <see cref="System.IFormatProvider"/> that supplies culture-specific formatting information.
    /// </param>
    /// <returns>
    /// The string representation of the value of this instance as specified by provider.
    /// </returns>
    public string ToString(IFormatProvider? provider)
    {
        return m_value.ToString(provider);
    }

    /// <summary>
    /// Converts the numeric value of this instance to its equivalent string representation using the
    /// specified format and culture-specific format information.
    /// </summary>
    /// <param name="format">A format specification.</param>
    /// <param name="provider">
    /// A <see cref="System.IFormatProvider"/> that supplies culture-specific formatting information.
    /// </param>
    /// <returns>
    /// The string representation of the value of this instance as specified by format and provider.
    /// </returns>
    public string ToString(string? format, IFormatProvider? provider)
    {
        return m_value.ToString(format, provider);
    }

    /// <summary>
    /// Converts the string representation of a number to its 24-bit signed integer equivalent.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <returns>
    /// A 24-bit signed integer equivalent to the number contained in s.
    /// </returns>
    /// <exception cref="ArgumentNullException">s is null.</exception>
    /// <exception cref="OverflowException">
    /// s represents a number less than Int24.MinValue or greater than Int24.MaxValue.
    /// </exception>
    /// <exception cref="FormatException">s is not in the correct format.</exception>
    public static Int24 Parse(string s)
    {
        return (Int24)int.Parse(s);
    }

    /// <summary>
    /// Converts the string representation of a number in a specified style to its 24-bit signed integer equivalent.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="style">
    /// A bitwise combination of System.Globalization.NumberStyles values that indicates the permitted format of s.
    /// A typical value to specify is System.Globalization.NumberStyles.Integer.
    /// </param>
    /// <returns>
    /// A 24-bit signed integer equivalent to the number contained in s.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// style is not a System.Globalization.NumberStyles value. -or- style is not a combination of
    /// System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber values.
    /// </exception>
    /// <exception cref="ArgumentNullException">s is null.</exception>
    /// <exception cref="OverflowException">
    /// s represents a number less than Int24.MinValue or greater than Int24.MaxValue.
    /// </exception>
    /// <exception cref="FormatException">s is not in a format compliant with style.</exception>
    public static Int24 Parse(string s, NumberStyles style)
    {
        return (Int24)int.Parse(s, style);
    }

    /// <summary>
    /// Converts the string representation of a number in a specified culture-specific format to its 24-bit
    /// signed integer equivalent.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="provider">
    /// A <see cref="System.IFormatProvider"/> that supplies culture-specific formatting information about s.
    /// </param>
    /// <returns>
    /// A 24-bit signed integer equivalent to the number contained in s.
    /// </returns>
    /// <exception cref="ArgumentNullException">s is null.</exception>
    /// <exception cref="OverflowException">
    /// s represents a number less than Int24.MinValue or greater than Int24.MaxValue.
    /// </exception>
    /// <exception cref="FormatException">s is not in the correct format.</exception>
    public static Int24 Parse(string s, IFormatProvider? provider)
    {
        return (Int24)int.Parse(s, provider);
    }

    /// <summary>
    /// Converts the string representation of a number in a specified style and culture-specific format to its 24-bit
    /// signed integer equivalent.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="style">
    /// A bitwise combination of System.Globalization.NumberStyles values that indicates the permitted format of s.
    /// A typical value to specify is System.Globalization.NumberStyles.Integer.
    /// </param>
    /// <param name="provider">
    /// A <see cref="System.IFormatProvider"/> that supplies culture-specific formatting information about s.
    /// </param>
    /// <returns>
    /// A 24-bit signed integer equivalent to the number contained in s.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// style is not a System.Globalization.NumberStyles value. -or- style is not a combination of
    /// System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber values.
    /// </exception>
    /// <exception cref="ArgumentNullException">s is null.</exception>
    /// <exception cref="OverflowException">
    /// s represents a number less than Int24.MinValue or greater than Int24.MaxValue.
    /// </exception>
    /// <exception cref="FormatException">s is not in a format compliant with style.</exception>
    public static Int24 Parse(string s, NumberStyles style, IFormatProvider? provider)
    {
        return (Int24)int.Parse(s, style, provider);
    }

    /// <summary>
    /// Converts the string representation of a number to its 24-bit signed integer equivalent. A return value
    /// indicates whether the conversion succeeded or failed.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="result">
    /// When this method returns, contains the 24-bit signed integer value equivalent to the number contained in s,
    /// if the conversion succeeded, or zero if the conversion failed. The conversion fails if the s parameter is null,
    /// is not of the correct format, or represents a number less than Int24.MinValue or greater than Int24.MaxValue.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>true if s was converted successfully; otherwise, false.</returns>
    public static bool TryParse(string s, out Int24 result)
    {
        bool parseResponse = int.TryParse(s, out int parseResult);

        try
        {
            result = (Int24)parseResult;
        }
        catch
        {
            result = (Int24)0;
            parseResponse = false;
        }

        return parseResponse;
    }


    /// <summary>
    /// Converts the string representation of a number in a specified style and culture-specific format to its
    /// 24-bit signed integer equivalent. A return value indicates whether the conversion succeeded or failed.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="style">
    /// A bitwise combination of System.Globalization.NumberStyles values that indicates the permitted format of s.
    /// A typical value to specify is System.Globalization.NumberStyles.Integer.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the 24-bit signed integer value equivalent to the number contained in s,
    /// if the conversion succeeded, or zero if the conversion failed. The conversion fails if the s parameter is null,
    /// is not in a format compliant with style, or represents a number less than Int24.MinValue or greater than
    /// Int24.MaxValue. This parameter is passed uninitialized.
    /// </param>
    /// <param name="provider">
    /// A <see cref="System.IFormatProvider"/> object that supplies culture-specific formatting information about s.
    /// </param>
    /// <returns>true if s was converted successfully; otherwise, false.</returns>
    /// <exception cref="ArgumentException">
    /// style is not a System.Globalization.NumberStyles value. -or- style is not a combination of
    /// System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber values.
    /// </exception>
    public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out Int24 result)
    {
        bool parseResponse = int.TryParse(s, style, provider, out int parseResult);

        try
        {
            result = (Int24)parseResult;
        }
        catch
        {
            result = (Int24)0;
            parseResponse = false;
        }

        return parseResponse;
    }

    /// <summary>
    /// Returns the System.TypeCode for value type System.Int32 (there is no defined type code for an Int24).
    /// </summary>
    /// <returns>The enumerated constant, System.TypeCode.Int32.</returns>
    /// <remarks>
    /// There is no defined Int24 type code and since an Int24 will easily fit inside an Int32, the
    /// Int32 type code is returned.
    /// </remarks>
    public TypeCode GetTypeCode()
    {
        return TypeCode.Int32;
    }

    #region [ Explicit IConvertible Implementation ]

    // These are explicitly implemented on the native integer implementations, so we do the same...

    bool IConvertible.ToBoolean(IFormatProvider? provider)
    {
        return Convert.ToBoolean(m_value, provider);
    }

    char IConvertible.ToChar(IFormatProvider? provider)
    {
        return Convert.ToChar(m_value, provider);
    }

    sbyte IConvertible.ToSByte(IFormatProvider? provider)
    {
        return Convert.ToSByte(m_value, provider);
    }

    byte IConvertible.ToByte(IFormatProvider? provider)
    {
        return Convert.ToByte(m_value, provider);
    }

    short IConvertible.ToInt16(IFormatProvider? provider)
    {
        return Convert.ToInt16(m_value, provider);
    }

    ushort IConvertible.ToUInt16(IFormatProvider? provider)
    {
        return Convert.ToUInt16(m_value, provider);
    }

    int IConvertible.ToInt32(IFormatProvider? provider)
    {
        return m_value;
    }

    uint IConvertible.ToUInt32(IFormatProvider? provider)
    {
        return Convert.ToUInt32(m_value, provider);
    }

    long IConvertible.ToInt64(IFormatProvider? provider)
    {
        return Convert.ToInt64(m_value, provider);
    }

    ulong IConvertible.ToUInt64(IFormatProvider? provider)
    {
        return Convert.ToUInt64(m_value, provider);
    }

    float IConvertible.ToSingle(IFormatProvider? provider)
    {
        return Convert.ToSingle(m_value, provider);
    }

    double IConvertible.ToDouble(IFormatProvider? provider)
    {
        return Convert.ToDouble(m_value, provider);
    }

    decimal IConvertible.ToDecimal(IFormatProvider? provider)
    {
        return Convert.ToDecimal(m_value, provider);
    }

    DateTime IConvertible.ToDateTime(IFormatProvider? provider)
    {
        return Convert.ToDateTime(m_value, provider);
    }

    object IConvertible.ToType(Type type, IFormatProvider? provider)
    {
        return Convert.ChangeType(m_value, type, provider);
    }

    #endregion

    #endregion

    #region [ Operators ]

    // Every effort has been made to make Int24 as cleanly interoperable with Int32 as possible...

    #region [ Comparison Operators ]

    /// <summary>
    /// Compares the two values for equality.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean value indicating equality.</returns>
    public static bool operator ==(Int24 value1, Int24 value2)
    {
        return value1.Equals(value2);
    }

    /// <summary>
    /// Compares the two values for equality.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean value indicating equality.</returns>
    public static bool operator ==(int value1, Int24 value2)
    {
        return value1.Equals(value2);
    }

    /// <summary>
    /// Compares the two values for equality.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean value indicating equality.</returns>
    public static bool operator ==(Int24 value1, int value2)
    {
        return ((int)value1).Equals(value2);
    }

    /// <summary>
    /// Compares the two values for inequality.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating the result of the inequality.</returns>
    public static bool operator !=(Int24 value1, Int24 value2)
    {
        return !value1.Equals(value2);
    }

    /// <summary>
    /// Compares the two values for inequality.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating the result of the inequality.</returns>
    public static bool operator !=(int value1, Int24 value2)
    {
        return !value1.Equals(value2);
    }

    /// <summary>
    /// Compares the two values for inequality.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating the result of the inequality.</returns>
    public static bool operator !=(Int24 value1, int value2)
    {
        return !((int)value1).Equals(value2);
    }

    /// <summary>
    /// Returns true if left value is less than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was less than the right value.</returns>
    public static bool operator <(Int24 value1, Int24 value2)
    {
        return value1.CompareTo(value2) < 0;
    }

    /// <summary>
    /// Returns true if left value is less than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was less than the right value.</returns>
    public static bool operator <(int value1, Int24 value2)
    {
        return value1.CompareTo(value2) < 0;
    }

    /// <summary>
    /// Returns true if left value is less than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was less than the right value.</returns>
    public static bool operator <(Int24 value1, int value2)
    {
        return value1.CompareTo(value2) < 0;
    }

    /// <summary>
    /// Returns true if left value is less or equal to than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was less than the right value.</returns>
    public static bool operator <=(Int24 value1, Int24 value2)
    {
        return value1.CompareTo(value2) <= 0;
    }

    /// <summary>
    /// Returns true if left value is less or equal to than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was less than the right value.</returns>
    public static bool operator <=(int value1, Int24 value2)
    {
        return value1.CompareTo(value2) <= 0;
    }

    /// <summary>
    /// Returns true if left value is less or equal to than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was less than the right value.</returns>
    public static bool operator <=(Int24 value1, int value2)
    {
        return value1.CompareTo(value2) <= 0;
    }

    /// <summary>
    /// Returns true if left value is greater than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was greater than the right value.</returns>
    public static bool operator >(Int24 value1, Int24 value2)
    {
        return value1.CompareTo(value2) > 0;
    }

    /// <summary>
    /// Returns true if left value is greater than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was greater than the right value.</returns>
    public static bool operator >(int value1, Int24 value2)
    {
        return value1.CompareTo(value2) > 0;
    }

    /// <summary>
    /// Returns true if left value is greater than right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was greater than the right value.</returns>
    public static bool operator >(Int24 value1, int value2)
    {
        return value1.CompareTo(value2) > 0;
    }

    /// <summary>
    /// Returns true if left value is greater than or equal to right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was greater than or equal to the right value.</returns>
    public static bool operator >=(Int24 value1, Int24 value2)
    {
        return value1.CompareTo(value2) >= 0;
    }

    /// <summary>
    /// Returns true if left value is greater than or equal to right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was greater than or equal to the right value.</returns>
    public static bool operator >=(int value1, Int24 value2)
    {
        return value1.CompareTo(value2) >= 0;
    }

    /// <summary>
    /// Returns true if left value is greater than or equal to right value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Boolean indicating whether the left value was greater than or equal to the right value.</returns>
    public static bool operator >=(Int24 value1, int value2)
    {
        return value1.CompareTo(value2) >= 0;
    }

    #endregion

    #region [ Type Conversion Operators ]

    #region [ Explicit Narrowing Conversions ]

    /// <summary>
    /// Explicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Enum value that is converted.</param>
    /// <returns>Int24</returns>
    public static explicit operator Int24(Enum value)
    {
        return new Int24(Convert.ToInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">String value that is converted.</param>
    /// <returns>Int24</returns>
    public static explicit operator Int24(string value)
    {
        return new Int24(Convert.ToInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Decimal value that is converted.</param>
    /// <returns>Int24</returns>
    public static explicit operator Int24(decimal value)
    {
        return new Int24(Convert.ToInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Double value that is converted.</param>
    /// <returns>Int24</returns>
    public static explicit operator Int24(double value)
    {
        return new Int24(Convert.ToInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Float value that is converted.</param>
    /// <returns>Int24</returns>
    public static explicit operator Int24(float value)
    {
        return new Int24(Convert.ToInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Long value that is converted.</param>
    /// <returns>Int24</returns>
    public static explicit operator Int24(long value)
    {
        return new Int24(Convert.ToInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Integer value that is converted.</param>
    /// <returns>Int24</returns>
    public static explicit operator Int24(int value)
    {
        return new Int24(value);
    }

    /// <summary>
    /// Explicitly converts <see cref="Int24"/> to <see cref="short"/>.
    /// </summary>
    /// <param name="value">Int24 value that is converted.</param>
    /// <returns>Short</returns>
    public static explicit operator short(Int24 value)
    {
        return (short)(int)value;
    }

    /// <summary>
    /// Explicitly converts <see cref="Int24"/> to <see cref="ushort"/>.
    /// </summary>
    /// <param name="value">Int24 value that is converted.</param>
    /// <returns>Unsigned Short</returns>
    public static explicit operator ushort(Int24 value)
    {
        return (ushort)(uint)value;
    }

    /// <summary>
    /// Explicitly converts <see cref="Int24"/> to <see cref="byte"/>.
    /// </summary>
    /// <param name="value">Int24 value that is converted.</param>
    /// <returns>Byte</returns>
    public static explicit operator byte(Int24 value)
    {
        return (byte)(int)value;
    }

    #endregion

    #region [ Implicit Widening Conversions ]

    /// <summary>
    /// Implicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Byte value that is converted to an <see cref="Int24"/>.</param>
    /// <returns>An <see cref="Int24"/> value.</returns>
    public static implicit operator Int24(byte value)
    {
        return new Int24((int)value);
    }

    /// <summary>
    /// Implicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Char value that is converted to an <see cref="Int24"/>.</param>
    /// <returns>An <see cref="Int24"/> value.</returns>
    public static implicit operator Int24(char value)
    {
        return new Int24((int)value);
    }

    /// <summary>
    /// Implicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value">Short value that is converted to an <see cref="Int24"/>.</param>
    /// <returns>An <see cref="Int24"/> value.</returns>
    public static implicit operator Int24(short value)
    {
        return new Int24((int)value);
    }

    /// <summary>
    /// Implicitly converts <see cref="Int24"/> to <see cref="int"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value that is converted to an <see cref="int"/>.</param>
    /// <returns>An <see cref="int"/> value.</returns>
    public static implicit operator int(Int24 value)
    {
        return ((IConvertible)value).ToInt32(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="Int24"/> to <see cref="uint"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value that is converted to an unsigned integer.</param>
    /// <returns>Unsigned integer</returns>
    public static implicit operator uint(Int24 value)
    {
        return ((IConvertible)value).ToUInt32(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="Int24"/> to <see cref="long"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value that is converted to an <see cref="long"/>.</param>
    /// <returns>An <see cref="long"/> value.</returns>
    public static implicit operator long(Int24 value)
    {
        return ((IConvertible)value).ToInt64(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="Int24"/> to <see cref="ulong"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value that is converted to an <see cref="ulong"/>.</param>
    /// <returns>An <see cref="ulong"/> value.</returns>
    public static implicit operator ulong(Int24 value)
    {
        return ((IConvertible)value).ToUInt64(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="Int24"/> to <see cref="double"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value that is converted to an <see cref="double"/>.</param>
    /// <returns>A <see cref="double"/> value.</returns>
    public static implicit operator double(Int24 value)
    {
        return ((IConvertible)value).ToDouble(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="Int24"/> to <see cref="float"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value that is converted to an <see cref="float"/>.</param>
    /// <returns>A <see cref="float"/> value.</returns>
    public static implicit operator float(Int24 value)
    {
        return ((IConvertible)value).ToSingle(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="Int24"/> to <see cref="decimal"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value that is converted to an <see cref="decimal"/>.</param>
    /// <returns>A <see cref="decimal"/> value.</returns>
    public static implicit operator decimal(Int24 value)
    {
        return ((IConvertible)value).ToDecimal(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="Int24"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value that is converted to an <see cref="string"/>.</param>
    /// <returns>A <see cref="string"/> value.</returns>
    public static implicit operator string(Int24 value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    #endregion

    #endregion

    #region [ Boolean and Bitwise Operators ]

    /// <summary>
    /// Returns true if value is not zero.
    /// </summary>
    /// <param name="value">Int24 value to test.</param>
    /// <returns>Boolean to indicate whether the value was not equal to zero.</returns>
    public static bool operator true(Int24 value)
    {
        return value != 0;
    }

    /// <summary>
    /// Returns true if value is equal to zero.
    /// </summary>
    /// <param name="value">Int24 value to test.</param>
    /// <returns>Boolean to indicate whether the value was equal to zero.</returns>
    public static bool operator false(Int24 value)
    {
        return value == 0;
    }

    /// <summary>
    /// Returns bitwise complement of value.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value as operand.</param>
    /// <returns><see cref="Int24"/> as result.</returns>
    public static Int24 operator ~(Int24 value)
    {
        return (Int24)ApplyBitMask(~(int)value);
    }

    /// <summary>
    /// Returns logical bitwise AND of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Int24 as result of operation.</returns>
    public static Int24 operator &(Int24 value1, Int24 value2)
    {
        return (Int24)ApplyBitMask((int)value1 & (int)value2);
    }

    /// <summary>
    /// Returns logical bitwise AND of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer as result of operation.</returns>
    public static int operator &(int value1, Int24 value2)
    {
        return value1 & (int)value2;
    }

    /// <summary>
    /// Returns logical bitwise AND of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer as result of operation.</returns>
    public static int operator &(Int24 value1, int value2)
    {
        return (int)value1 & value2;
    }

    /// <summary>
    /// Returns logical bitwise OR of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Int24 as result of operation.</returns>
    public static Int24 operator |(Int24 value1, Int24 value2)
    {
        return (Int24)ApplyBitMask((int)value1 | (int)value2);
    }

    /// <summary>
    /// Returns logical bitwise OR of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer as result of operation.</returns>
    public static int operator |(int value1, Int24 value2)
    {
        return value1 | (int)value2;
    }

    /// <summary>
    /// Returns logical bitwise OR of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer as result of operation.</returns>
    public static int operator |(Int24 value1, int value2)
    {
        return (int)value1 | value2;
    }

    /// <summary>
    /// Returns logical bitwise exclusive-OR of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer value of the resulting exclusive-OR operation.</returns>
    public static Int24 operator ^(Int24 value1, Int24 value2)
    {
        return (Int24)ApplyBitMask((int)value1 ^ (int)value2);
    }

    /// <summary>
    /// Returns logical bitwise exclusive-OR of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer value of the resulting exclusive-OR operation.</returns>
    public static int operator ^(int value1, Int24 value2)
    {
        return value1 ^ (int)value2;
    }

    /// <summary>
    /// Returns logical bitwise exclusive-OR of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer value of the resulting exclusive-OR operation.</returns>
    public static int operator ^(Int24 value1, int value2)
    {
        return (int)value1 ^ value2;
    }

    /// <summary>
    /// Returns value after right shifts of first value by the number of bits specified by second value.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value to shift.</param>
    /// <param name="shifts"><see cref="int"/> shifts indicates how many places to shift.</param>
    /// <returns>An <see cref="Int24"/> value.</returns>
    public static Int24 operator >>(Int24 value, int shifts)
    {
        return (Int24)ApplyBitMask((int)value >> shifts);
    }

    /// <summary>
    /// Returns value after left shifts of first value by the number of bits specified by second value.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value to shift.</param>
    /// <param name="shifts"><see cref="int"/> shifts indicates how many places to shift.</param>
    /// <returns>An <see cref="Int24"/> value.</returns>
    public static Int24 operator <<(Int24 value, int shifts)
    {
        return (Int24)ApplyBitMask((int)value << shifts);
    }

    #endregion

    #region [ Arithmetic Operators ]

    /// <summary>
    /// Returns computed remainder after dividing first value by the second.
    /// </summary>
    /// <param name="value1"><see cref="Int24"/> value as numerator.</param>
    /// <param name="value2"><see cref="Int24"/> value as denominator.</param>
    /// <returns><see cref="Int24"/> as remainder</returns>
    public static Int24 operator %(Int24 value1, Int24 value2)
    {
        return (Int24)((int)value1 % (int)value2);
    }

    /// <summary>
    /// Returns computed remainder after dividing first value by the second.
    /// </summary>
    /// <param name="value1"><see cref="int"/> value as numerator.</param>
    /// <param name="value2"><see cref="Int24"/> value as denominator.</param>
    /// <returns><see cref="int"/> as remainder</returns>
    public static int operator %(int value1, Int24 value2)
    {
        return value1 % (int)value2;
    }

    /// <summary>
    /// Returns computed remainder after dividing first value by the second.
    /// </summary>
    /// <param name="value1"><see cref="Int24"/> value as numerator.</param>
    /// <param name="value2"><see cref="int"/> value as denominator.</param>
    /// <returns><see cref="int"/> as remainder</returns>
    public static int operator %(Int24 value1, int value2)
    {
        return (int)value1 % value2;
    }

    /// <summary>
    /// Returns computed sum of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Int24 result of addition.</returns>
    public static Int24 operator +(Int24 value1, Int24 value2)
    {
        return (Int24)((int)value1 + (int)value2);
    }

    /// <summary>
    /// Returns computed sum of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer result of addition.</returns>
    public static int operator +(int value1, Int24 value2)
    {
        return value1 + (int)value2;
    }

    /// <summary>
    /// Returns computed sum of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer result of addition.</returns>
    public static int operator +(Int24 value1, int value2)
    {
        return (int)value1 + value2;
    }

    /// <summary>
    /// Returns computed difference of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Int24 result of subtraction.</returns>
    public static Int24 operator -(Int24 value1, Int24 value2)
    {
        return (Int24)((int)value1 - (int)value2);
    }

    /// <summary>
    /// Returns computed difference of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer result of subtraction.</returns>
    public static int operator -(int value1, Int24 value2)
    {
        return value1 - (int)value2;
    }

    /// <summary>
    /// Returns computed difference of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer result of subtraction.</returns>
    public static int operator -(Int24 value1, int value2)
    {
        return (int)value1 - value2;
    }

    /// <summary>
    /// Returns incremented value.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>Int24 result of increment.</returns>
    public static Int24 operator ++(Int24 value)
    {
        return (Int24)(value + 1);
    }

    /// <summary>
    /// Returns decremented value.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>Int24 result of decrement.</returns>
    public static Int24 operator --(Int24 value)
    {
        return (Int24)(value - 1);
    }

    /// <summary>
    /// Returns computed product of values.
    /// </summary>
    /// <param name="value1"><see cref="Int24"/> value as left hand operand.</param>
    /// <param name="value2"><see cref="Int24"/> value as right hand operand.</param>
    /// <returns><see cref="Int24"/> as result</returns>
    public static Int24 operator *(Int24 value1, Int24 value2)
    {
        return (Int24)((int)value1 * (int)value2);
    }

    /// <summary>
    /// Returns computed product of values.
    /// </summary>
    /// <param name="value1"><see cref="int"/> value as left hand operand.</param>
    /// <param name="value2"><see cref="Int24"/> value as right hand operand.</param>
    /// <returns><see cref="int"/> as result</returns>
    public static int operator *(int value1, Int24 value2)
    {
        return value1 * (int)value2;
    }

    /// <summary>
    /// Returns computed product of values.
    /// </summary>
    /// <param name="value1"><see cref="Int24"/> value as left hand operand.</param>
    /// <param name="value2"><see cref="int"/> value as right hand operand.</param>
    /// <returns><see cref="int"/> as result</returns>
    public static int operator *(Int24 value1, int value2)
    {
        return (int)value1 * value2;
    }

    // Integer division operators

    /// <summary>
    /// Returns computed division of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Int24 result of operation.</returns>
    public static Int24 operator /(Int24 value1, Int24 value2)
    {
        return (Int24)((int)value1 / (int)value2);
    }

    /// <summary>
    /// Returns computed division of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer result of operation.</returns>
    public static int operator /(int value1, Int24 value2)
    {
        return value1 / (int)value2;
    }

    /// <summary>
    /// Returns computed division of values.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Integer result of operation.</returns>
    public static int operator /(Int24 value1, int value2)
    {
        return (int)value1 / value2;
    }

    //// Standard division operators
    //public static double operator /(Int24 value1, Int24 value2)
    //{
    //    return ((double)value1 / (double)value2);
    //}

    //public static double operator /(int value1, Int24 value2)
    //{
    //    return ((double)value1 / (double)value2);
    //}

    //public static double operator /(Int24 value1, int value2)
    //{
    //    return ((double)value1 / (double)value2);
    //}

    // C# doesn't expose an exponent operator but some other .NET languages do,
    // so we expose the operator via its native special IL function name

    /// <summary>
    /// Returns result of first value raised to power of second value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Double that is the result of the operation.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced), SpecialName]
    public static double op_Exponent(Int24 value1, Int24 value2)
    {
        return Math.Pow(value1, value2);
    }

    /// <summary>
    /// Returns result of first value raised to power of second value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Double that is the result of the operation.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced), SpecialName]
    public static double op_Exponent(int value1, Int24 value2)
    {
        return Math.Pow(value1, value2);
    }

    /// <summary>
    /// Returns result of first value raised to power of second value.
    /// </summary>
    /// <param name="value1">Left hand operand.</param>
    /// <param name="value2">Right hand operand.</param>
    /// <returns>Double that is the result of the operation.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced), SpecialName]
    public static double op_Exponent(Int24 value1, int value2)
    {
        return Math.Pow(value1, value2);
    }

    #endregion

    #endregion

    #region [ Static ]

    /// <summary>
    /// Represents the largest possible value of an Int24. This field is constant.
    /// </summary>
    public static readonly Int24 MaxValue = (Int24)MaxValue32;

    /// <summary>
    /// Represents the smallest possible value of an Int24. This field is constant.
    /// </summary>
    public static readonly Int24 MinValue = (Int24)MinValue32;

    /// <summary>Returns the specified Int24 value as an array of three bytes.</summary>
    /// <param name="value">Int24 value to convert to bytes.</param>
    /// <returns>An array of bytes with length 3.</returns>
    /// <remarks>
    /// <para>You can use this function in-lieu of a System.BitConverter.GetBytes(Int24) function.</para>
    /// <para>Bytes will be returned in endian order of currently executing process architecture (little-endian on Intel platforms).</para>
    /// </remarks>
    public static byte[] GetBytes(Int24 value)
    {
        // We use a 32-bit integer to store 24-bit integer internally
        byte[] data = new byte[3];
        int valueInt = value;

        if (BitConverter.IsLittleEndian)
        {
            data[0] = (byte)valueInt;
            data[1] = (byte)(valueInt >> 8);
            data[2] = (byte)(valueInt >> 16);
        }
        else
        {
            data[0] = (byte)(valueInt >> 16);
            data[1] = (byte)(valueInt >> 8);
            data[2] = (byte)valueInt;
        }

        // Return serialized 3-byte representation of Int24
        return data;
    }

    /// <summary>Returns a 24-bit signed integer from three bytes at a specified position in a byte array.</summary>
    /// <param name="value">An array of bytes.</param>
    /// <param name="startIndex">The starting position within value.</param>
    /// <returns>A 24-bit signed integer formed by three bytes beginning at startIndex.</returns>
    /// <remarks>
    /// <para>You can use this function in-lieu of a System.BitConverter.ToInt24 function.</para>
    /// <para>Bytes endian order assumed to match that of currently executing process architecture (little-endian on Intel platforms).</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is greater than <paramref name="value"/> length.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> length from <paramref name="startIndex"/> is too small to represent an <see cref="Int24"/>.</exception>
    public static Int24 GetValue(byte[] value, int startIndex)
    {
        value.ValidateParameters(startIndex, 3);
        int valueInt;

        if (BitConverter.IsLittleEndian)
        {
            valueInt = value[startIndex] |
                       value[startIndex + 1] << 8 |
                       value[startIndex + 2] << 16;
        }
        else
        {
            valueInt = value[startIndex] << 16 |
                       value[startIndex + 1] << 8 |
                       value[startIndex + 2];
        }

        // Deserialize value
        return (Int24)ApplyBitMask(valueInt);
    }

    private static void ValidateNumericRange(int value)
    {
        if (value is > MaxValue32 + 1 or < MinValue32)
            throw new OverflowException($"Value of {value} will not fit in a 24-bit signed integer");
    }

    private static int ApplyBitMask(int value)
    {
        // Check bit 23, the sign bit in a signed 24-bit integer
        if ((value & 0x00800000) > 0)
        {
            // If the sign-bit is set, this number will be negative - set all high-byte bits (keeps 32-bit number in 24-bit range)
            value |= BitMask;
        }
        else
        {
            // If the sign-bit is not set, this number will be positive - clear all high-byte bits (keeps 32-bit number in 24-bit range)
            value &= ~BitMask;
        }

        return value;
    }

    #endregion
}
