//******************************************************************************************************
//  UInt24.cs - Gbtc
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
//  08/4/2009 - Josh L. Patterson
//       Edited Code Comments.
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

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>Represents a 3-byte, 24-bit unsigned integer.</summary>
/// <remarks>
/// <para>
/// This class behaves like most other intrinsic unsigned integers but allows a 3-byte, 24-bit integer implementation
/// that is often found in many digital-signal processing arenas and different kinds of protocol parsing.  An unsigned
/// 24-bit integer is typically used to save storage space on disk where its value range of 0 to 16777215 is sufficient,
/// but the unsigned Int16 value range of 0 to 65535 is too small.
/// </para>
/// <para>
/// This structure uses an UInt32 internally for storage and most other common expected integer functionality, so using
/// a 24-bit integer will not save memory.  However, if the 24-bit unsigned integer range (0 to 16777215) suits your
/// data needs you can save disk space by only storing the three bytes that this integer actually consumes.  You can do
/// this by calling the UInt24.GetBytes function to return a three byte binary array that can be serialized to the desired
/// destination and then calling the UInt24.GetValue function to restore the UInt24 value from those three bytes.
/// </para>
/// <para>
/// All the standard operators for the UInt24 have been fully defined for use with both UInt24 and UInt32 unsigned integers;
/// you should find that without the exception UInt24 can be compared and numerically calculated with an UInt24 or UInt32.
/// Necessary casting should be minimal and typical use should be very simple - just as if you are using any other native
/// unsigned integer.
/// </para>
/// </remarks>
[Serializable]
public struct UInt24 : IComparable, IFormattable, IConvertible, IComparable<UInt24>, IComparable<uint>, IEquatable<UInt24>, IEquatable<uint>
{
    #region [ Members ]

    // Constants
    private const uint MaxValue32 = 0x00ffffff; // Represents the largest possible value of an UInt24 as an UInt32.
    private const uint MinValue32 = 0x00000000; // Represents the smallest possible value of an UInt24 as an UInt32.

    /// <summary>High byte bit-mask used when a 24-bit integer is stored within a 32-bit integer. This field is constant.</summary>
    public const uint BitMask = 0xff000000;

    // Fields
    private readonly uint m_value; // We internally store the UInt24 value in a 4-byte unsigned integer for convenience

    #endregion

    #region [ Constructors ]

    /// <summary>Creates 24-bit unsigned integer from an existing 24-bit unsigned integer.</summary>
    /// <param name="value">A <see cref="UInt24"/> to create the new value from. </param>
    public UInt24(UInt24 value)
    {
        m_value = ApplyBitMask(value);
    }

    /// <summary>Creates 24-bit unsigned integer from a 32-bit unsigned integer.</summary>
    /// <param name="value">32-bit unsigned integer to use as new 24-bit unsigned integer value.</param>
    /// <exception cref="OverflowException">Source values over 24-bit max range will cause an overflow exception.</exception>
    public UInt24(uint value)
    {
        ValidateNumericRange(value);
        m_value = ApplyBitMask(value);
    }

    /// <summary>Creates 24-bit unsigned integer from three bytes at a specified position in a byte array.</summary>
    /// <param name="value">An array of bytes.</param>
    /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
    /// <remarks>
    /// <para>You can use this constructor in-lieu of a System.BitConverter.ToUInt24 function.</para>
    /// <para>Bytes endian order assumed to match that of currently executing process architecture (little-endian on Intel platforms).</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is greater than <paramref name="value"/> length.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> length from <paramref name="startIndex"/> is too small to represent a <see cref="UInt24"/>.</exception>
    public UInt24(byte[] value, int startIndex)
    {
        m_value = GetValue(value, startIndex).m_value;
    }

    #endregion

    #region [ Methods ]

    /// <summary>Returns the UInt24 value as an array of three bytes.</summary>
    /// <returns>An array of bytes with length 3.</returns>
    /// <remarks>
    /// <para>You can use this function in-lieu of a System.BitConverter.GetBytes function.</para>
    /// <para>Bytes will be returned in endian order of currently executing process architecture (little-endian on Intel platforms).</para>
    /// </remarks>
    public byte[] GetBytes()
    {
        // Return serialized 3-byte representation of UInt24
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
    /// <exception cref="ArgumentException">value is not an UInt32 or UInt24.</exception>
    public int CompareTo(object? value)
    {
        if (value is null)
            return 1;

        if (value is not uint && value is not UInt24)
            throw new ArgumentException("Argument must be an UInt32 or an UInt24");

        uint num = (uint)value;

        return m_value < num ? -1 : m_value > num ? 1 : 0;
    }

    /// <summary>
    /// Compares this instance to a specified 24-bit unsigned integer and returns an indication of their
    /// relative values.
    /// </summary>
    /// <param name="value">An integer to compare.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and value. Returns less than zero
    /// if this instance is less than value, zero if this instance is equal to value, or greater than zero
    /// if this instance is greater than value.
    /// </returns>
    public int CompareTo(UInt24 value)
    {
        return CompareTo((uint)value);
    }

    /// <summary>
    /// Compares this instance to a specified 32-bit unsigned integer and returns an indication of their
    /// relative values.
    /// </summary>
    /// <param name="value">An integer to compare.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and value. Returns less than zero
    /// if this instance is less than value, zero if this instance is equal to value, or greater than zero
    /// if this instance is greater than value.
    /// </returns>
    public int CompareTo(uint value)
    {
        return m_value < value ? -1 : m_value > value ? 1 : 0;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="obj">An object to compare, or null.</param>
    /// <returns>
    /// True if obj is an instance of UInt32 or UInt24 and equals the value of this instance;
    /// otherwise, False.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is uint or UInt24)
            return Equals((uint)obj);

        return false;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified UInt24 value.
    /// </summary>
    /// <param name="obj">An UInt24 value to compare to this instance.</param>
    /// <returns>
    /// True if obj has the same value as this instance; otherwise, False.
    /// </returns>
    public bool Equals(UInt24 obj)
    {
        return Equals((uint)obj);
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified uint value.
    /// </summary>
    /// <param name="obj">An UInt32 value to compare to this instance.</param>
    /// <returns>
    /// True if obj has the same value as this instance; otherwise, False.
    /// </returns>
    public bool Equals(uint obj)
    {
        return m_value == obj;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>
    /// A 32-bit unsigned integer hash code.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (int)m_value;
        }
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
    /// Converts the string representation of a number to its 24-bit unsigned integer equivalent.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <returns>
    /// A 24-bit unsigned integer equivalent to the number contained in s.
    /// </returns>
    /// <exception cref="ArgumentNullException">s is null.</exception>
    /// <exception cref="OverflowException">
    /// s represents a number less than UInt24.MinValue or greater than UInt24.MaxValue.
    /// </exception>
    /// <exception cref="FormatException">s is not in the correct format.</exception>
    public static UInt24 Parse(string s)
    {
        return (UInt24)uint.Parse(s);
    }

    /// <summary>
    /// Converts the string representation of a number in a specified style to its 24-bit unsigned integer equivalent.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="style">
    /// A bitwise combination of System.Globalization.NumberStyles values that indicates the permitted format of s.
    /// A typical value to specify is System.Globalization.NumberStyles.Integer.
    /// </param>
    /// <returns>
    /// A 24-bit unsigned integer equivalent to the number contained in s.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// style is not a System.Globalization.NumberStyles value. -or- style is not a combination of
    /// System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber values.
    /// </exception>
    /// <exception cref="ArgumentNullException">s is null.</exception>
    /// <exception cref="OverflowException">
    /// s represents a number less than UInt24.MinValue or greater than UInt24.MaxValue.
    /// </exception>
    /// <exception cref="FormatException">s is not in a format compliant with style.</exception>
    public static UInt24 Parse(string s, NumberStyles style)
    {
        return (UInt24)uint.Parse(s, style);
    }

    /// <summary>
    /// Converts the string representation of a number in a specified culture-specific format to its 24-bit
    /// unsigned integer equivalent.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="provider">
    /// A <see cref="System.IFormatProvider"/> that supplies culture-specific formatting information about s.
    /// </param>
    /// <returns>
    /// A 24-bit unsigned integer equivalent to the number contained in s.
    /// </returns>
    /// <exception cref="ArgumentNullException">s is null.</exception>
    /// <exception cref="OverflowException">
    /// s represents a number less than UInt24.MinValue or greater than UInt24.MaxValue.
    /// </exception>
    /// <exception cref="FormatException">s is not in the correct format.</exception>
    public static UInt24 Parse(string s, IFormatProvider? provider)
    {
        return (UInt24)uint.Parse(s, provider);
    }

    /// <summary>
    /// Converts the string representation of a number in a specified style and culture-specific format to its 24-bit
    /// unsigned integer equivalent.
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
    /// A 24-bit unsigned integer equivalent to the number contained in s.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// style is not a System.Globalization.NumberStyles value. -or- style is not a combination of
    /// System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber values.
    /// </exception>
    /// <exception cref="ArgumentNullException">s is null.</exception>
    /// <exception cref="OverflowException">
    /// s represents a number less than UInt24.MinValue or greater than UInt24.MaxValue.
    /// </exception>
    /// <exception cref="FormatException">s is not in a format compliant with style.</exception>
    public static UInt24 Parse(string s, NumberStyles style, IFormatProvider? provider)
    {
        return (UInt24)uint.Parse(s, style, provider);
    }

    /// <summary>
    /// Converts the string representation of a number to its 24-bit unsigned integer equivalent. A return value
    /// indicates whether the conversion succeeded or failed.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="result">
    /// When this method returns, contains the 24-bit unsigned integer value equivalent to the number contained in s,
    /// if the conversion succeeded, or zero if the conversion failed. The conversion fails if the s parameter is null,
    /// is not of the correct format, or represents a number less than UInt24.MinValue or greater than UInt24.MaxValue.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>true if s was converted successfully; otherwise, false.</returns>
    public static bool TryParse(string s, out UInt24 result)
    {
        bool parseResponse = uint.TryParse(s, out uint parseResult);

        try
        {
            result = (UInt24)parseResult;
        }
        catch
        {
            result = 0;
            parseResponse = false;
        }

        return parseResponse;
    }

    /// <summary>
    /// Converts the string representation of a number in a specified style and culture-specific format to its
    /// 24-bit unsigned integer equivalent. A return value indicates whether the conversion succeeded or failed.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="style">
    /// A bitwise combination of System.Globalization.NumberStyles values that indicates the permitted format of s.
    /// A typical value to specify is System.Globalization.NumberStyles.Integer.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the 24-bit unsigned integer value equivalent to the number contained in s,
    /// if the conversion succeeded, or zero if the conversion failed. The conversion fails if the s parameter is null,
    /// is not in a format compliant with style, or represents a number less than UInt24.MinValue or greater than
    /// UInt24.MaxValue. This parameter is passed uninitialized.
    /// </param>
    /// <param name="provider">
    /// A <see cref="System.IFormatProvider"/> object that supplies culture-specific formatting information about s.
    /// </param>
    /// <returns>true if s was converted successfully; otherwise, false.</returns>
    /// <exception cref="ArgumentException">
    /// style is not a System.Globalization.NumberStyles value. -or- style is not a combination of
    /// System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber values.
    /// </exception>
    public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out UInt24 result)
    {
        bool parseResponse = uint.TryParse(s, style, provider, out uint parseResult);

        try
        {
            result = (UInt24)parseResult;
        }
        catch
        {
            result = 0;
            parseResponse = false;
        }

        return parseResponse;
    }

    /// <summary>
    /// Returns the System.TypeCode for value type System.UInt32 (there is no defined type code for an UInt24).
    /// </summary>
    /// <returns>The enumerated constant, System.TypeCode.UInt32.</returns>
    /// <remarks>
    /// There is no defined UInt24 type code and since an UInt24 will easily fit inside an UInt32, the
    /// UInt32 type code is returned.
    /// </remarks>
    public TypeCode GetTypeCode()
    {
        return TypeCode.UInt32;
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
        return Convert.ToInt32(m_value, provider);
    }

    uint IConvertible.ToUInt32(IFormatProvider? provider)
    {
        return m_value;
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

    // Every effort has been made to make UInt24 as cleanly interoperable with UInt32 as possible...

    #region [ Comparison Operators ]

    /// <summary>
    /// Compares the two values for equality.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator ==(UInt24 value1, UInt24 value2)
    {
        return value1.Equals(value2);
    }

    /// <summary>
    /// Compares the two values for equality.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator ==(uint value1, UInt24 value2)
    {
        return value1.Equals(value2);
    }

    /// <summary>
    /// Compares the two values for equality.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator ==(UInt24 value1, uint value2)
    {
        return ((uint)value1).Equals(value2);
    }

    /// <summary>
    /// Compares the two values for inequality.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator !=(UInt24 value1, UInt24 value2)
    {
        return !value1.Equals(value2);
    }

    /// <summary>
    /// Compares the two values for inequality.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator !=(uint value1, UInt24 value2)
    {
        return !value1.Equals(value2);
    }

    /// <summary>
    /// Compares the two values for inequality.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator !=(UInt24 value1, uint value2)
    {
        return !((uint)value1).Equals(value2);
    }

    /// <summary>
    /// Returns true if left value is less than right value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator <(UInt24 value1, UInt24 value2)
    {
        return value1.CompareTo(value2) < 0;
    }

    /// <summary>
    /// Returns true if left value is less than right value.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator <(uint value1, UInt24 value2)
    {
        return value1.CompareTo(value2) < 0;
    }

    /// <summary>
    /// Returns true if left value is less than right value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator <(UInt24 value1, uint value2)
    {
        return value1.CompareTo(value2) < 0;
    }

    /// <summary>
    /// Returns true if left value is less or equal to than right value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator <=(UInt24 value1, UInt24 value2)
    {
        return value1.CompareTo(value2) <= 0;
    }

    /// <summary>
    /// Returns true if left value is less or equal to than right value.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator <=(uint value1, UInt24 value2)
    {
        return value1.CompareTo(value2) <= 0;
    }

    /// <summary>
    /// Returns true if left value is less or equal to than right value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator <=(UInt24 value1, uint value2)
    {
        return value1.CompareTo(value2) <= 0;
    }

    /// <summary>
    /// Returns true if left value is greater than right value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator >(UInt24 value1, UInt24 value2)
    {
        return value1.CompareTo(value2) > 0;
    }

    /// <summary>
    /// Returns true if left value is greater than right value.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator >(uint value1, UInt24 value2)
    {
        return value1.CompareTo(value2) > 0;
    }

    /// <summary>
    /// Returns true if left value is greater than right value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator >(UInt24 value1, uint value2)
    {
        return value1.CompareTo(value2) > 0;
    }

    /// <summary>
    /// Returns true if left value is greater than or equal to right value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator >=(UInt24 value1, UInt24 value2)
    {
        return value1.CompareTo(value2) >= 0;
    }

    /// <summary>
    /// Returns true if left value is greater than or equal to right value.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator >=(uint value1, UInt24 value2)
    {
        return value1.CompareTo(value2) >= 0;
    }

    /// <summary>
    /// Returns true if left value is greater than or equal to right value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="bool"/> value representing the result.</returns>
    public static bool operator >=(UInt24 value1, uint value2)
    {
        return value1.CompareTo(value2) >= 0;
    }

    #endregion

    #region [ Type Conversion Operators ]

    #region [ Explicit Narrowing Conversions ]

    /// <summary>
    /// Explicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="Enum"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static explicit operator UInt24(Enum value)
    {
        return new UInt24(Convert.ToUInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static explicit operator UInt24(string value)
    {
        return new UInt24(Convert.ToUInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="decimal"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static explicit operator UInt24(decimal value)
    {
        return new UInt24(Convert.ToUInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="double"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static explicit operator UInt24(double value)
    {
        return new UInt24(Convert.ToUInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="float"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static explicit operator UInt24(float value)
    {
        return new UInt24(Convert.ToUInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="ulong"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static explicit operator UInt24(ulong value)
    {
        return new UInt24(Convert.ToUInt32(value));
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="uint"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static explicit operator UInt24(uint value)
    {
        return new UInt24(value);
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="Int24"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static explicit operator UInt24(Int24 value)
    {
        return new UInt24(value);
    }

    /// <summary>
    /// Explicitly converts value to an <see cref="Int24"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="Int24"/> value that is the result of the conversion.</returns>
    public static explicit operator Int24(UInt24 value)
    {
        return new Int24(value);
    }

    /// <summary>
    /// Explicitly converts <see cref="UInt24"/> to <see cref="short"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="short"/> value that is the result of the conversion.</returns>
    public static explicit operator short(UInt24 value)
    {
        return (short)(uint)value;
    }

    /// <summary>
    /// Explicitly converts <see cref="UInt24"/> to <see cref="ushort"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="ushort"/> value that is the result of the conversion.</returns>
    public static explicit operator ushort(UInt24 value)
    {
        return (ushort)(uint)value;
    }

    /// <summary>
    /// Explicitly converts <see cref="UInt24"/> to <see cref="byte"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="byte"/> value that is the result of the conversion.</returns>
    public static explicit operator byte(UInt24 value)
    {
        return (byte)(uint)value;
    }

    #endregion

    #region [ Implicit Widening Conversions ]

    /// <summary>
    /// Implicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="byte"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static implicit operator UInt24(byte value)
    {
        return new UInt24((uint)value);
    }

    /// <summary>
    /// Implicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="char"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static implicit operator UInt24(char value)
    {
        return new UInt24((uint)value);
    }

    /// <summary>
    /// Implicitly converts value to an <see cref="UInt24"/>.
    /// </summary>
    /// <param name="value"><see cref="ushort"/> value to be converted.</param>
    /// <returns><see cref="UInt24"/> value that is the result of the conversion.</returns>
    public static implicit operator UInt24(ushort value)
    {
        return new UInt24((uint)value);
    }

    /// <summary>
    /// Implicitly converts <see cref="UInt24"/> to <see cref="int"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="int"/> value that is the result of the conversion.</returns>
    public static implicit operator int(UInt24 value)
    {
        return ((IConvertible)value).ToInt32(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="UInt24"/> to <see cref="uint"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="uint"/> value that is the result of the conversion.</returns>
    public static implicit operator uint(UInt24 value)
    {
        return ((IConvertible)value).ToUInt32(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="UInt24"/> to <see cref="long"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="long"/> value that is the result of the conversion.</returns>
    public static implicit operator long(UInt24 value)
    {
        return ((IConvertible)value).ToInt64(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="UInt24"/> to <see cref="ulong"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="ulong"/> value that is the result of the conversion.</returns>
    public static implicit operator ulong(UInt24 value)
    {
        return ((IConvertible)value).ToUInt64(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="UInt24"/> to <see cref="double"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="double"/> value that is the result of the conversion.</returns>
    public static implicit operator double(UInt24 value)
    {
        return ((IConvertible)value).ToDouble(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="UInt24"/> to <see cref="float"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="float"/> value that is the result of the conversion.</returns>
    public static implicit operator float(UInt24 value)
    {
        return ((IConvertible)value).ToSingle(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="UInt24"/> to <see cref="decimal"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="decimal"/> value that is the result of the conversion.</returns>
    public static implicit operator decimal(UInt24 value)
    {
        return ((IConvertible)value).ToDecimal(null);
    }

    /// <summary>
    /// Implicitly converts <see cref="UInt24"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to be converted.</param>
    /// <returns><see cref="string"/> value that is the result of the conversion.</returns>
    public static implicit operator string(UInt24 value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    #endregion

    #endregion

    #region [ Boolean and Bitwise Operators ]

    /// <summary>
    /// Returns true if value is greater than zero.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to evaluate.</param>
    /// <returns><see cref="bool"/> value indicating whether the value is greater than zero.</returns>
    public static bool operator true(UInt24 value)
    {
        return value > 0;
    }

    /// <summary>
    /// Returns true if value is equal to zero.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to evaluate.</param>
    /// <returns><see cref="bool"/> value indicating whether the value is equal than zero.</returns>
    public static bool operator false(UInt24 value)
    {
        return value == 0;
    }

    /// <summary>
    /// Returns bitwise complement of value.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to evaluate.</param>
    /// <returns><see cref="UInt24"/> value representing the complement of the input value.</returns>
    public static UInt24 operator ~(UInt24 value)
    {
        return (UInt24)ApplyBitMask(~(uint)value);
    }

    /// <summary>
    /// Returns logical bitwise AND of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="UInt24"/> value representing the logical bitwise AND of the values.</returns>
    public static UInt24 operator &(UInt24 value1, UInt24 value2)
    {
        return (UInt24)ApplyBitMask((uint)value1 & (uint)value2);
    }

    /// <summary>
    /// Returns logical bitwise AND of values.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value representing the logical bitwise AND of the values.</returns>
    public static uint operator &(uint value1, UInt24 value2)
    {
        return value1 & (uint)value2;
    }

    /// <summary>
    /// Returns logical bitwise AND of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value representing the logical bitwise AND of the values.</returns>
    public static uint operator &(UInt24 value1, uint value2)
    {
        return (uint)value1 & value2;
    }

    /// <summary>
    /// Returns logical bitwise OR of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="UInt24"/> value representing the logical bitwise OR of the values.</returns>
    public static UInt24 operator |(UInt24 value1, UInt24 value2)
    {
        return (UInt24)ApplyBitMask((uint)value1 | (uint)value2);
    }

    /// <summary>
    /// Returns logical bitwise OR of values.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value representing the logical bitwise OR of the values.</returns>
    public static uint operator |(uint value1, UInt24 value2)
    {
        return value1 | (uint)value2;
    }

    /// <summary>
    /// Returns logical bitwise OR of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value representing the logical bitwise OR of the values.</returns>
    public static uint operator |(UInt24 value1, uint value2)
    {
        return (uint)value1 | value2;
    }

    /// <summary>
    /// Returns logical bitwise exclusive-OR of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="UInt24"/> value representing the logical bitwise exclusive-OR of the values.</returns>
    public static UInt24 operator ^(UInt24 value1, UInt24 value2)
    {
        return (UInt24)ApplyBitMask((uint)value1 ^ (uint)value2);
    }

    /// <summary>
    /// Returns logical bitwise exclusive-OR of values.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value representing the logical bitwise exclusive-OR of the values.</returns>
    public static uint operator ^(uint value1, UInt24 value2)
    {
        return value1 ^ (uint)value2;
    }

    /// <summary>
    /// Returns logical bitwise exclusive-OR of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value representing the logical bitwise exclusive-OR of the values.</returns>
    public static uint operator ^(UInt24 value1, uint value2)
    {
        return (uint)value1 ^ value2;
    }

    /// <summary>
    /// Returns value after right shifts of first value by the number of bits specified by second value.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to right shift.</param>
    /// <param name="shifts"><see cref="int"/> value indicating the number of bits to right shift by.</param>
    /// <returns><see cref="UInt24"/> value as result of right shift operation.</returns>
    public static UInt24 operator >>(UInt24 value, int shifts)
    {
        return (UInt24)ApplyBitMask((uint)value >> shifts);
    }

    /// <summary>
    /// Returns value after left shifts of first value by the number of bits specified by second value.
    /// </summary>
    /// <param name="value"><see cref="UInt24"/> value to left shift.</param>
    /// <param name="shifts"><see cref="int"/> value indicating the number of bits to left shift by.</param>
    /// <returns><see cref="UInt24"/> value as result of left shift operation.</returns>
    public static UInt24 operator <<(UInt24 value, int shifts)
    {
        return (UInt24)ApplyBitMask((uint)value << shifts);
    }

    #endregion

    #region [ Arithmetic Operators ]

    /// <summary>
    /// Returns computed remainder after dividing first value by the second.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="UInt24"/> value as result of modulus operation.</returns>
    public static UInt24 operator %(UInt24 value1, UInt24 value2)
    {
        return (UInt24)((uint)value1 % (uint)value2);
    }

    /// <summary>
    /// Returns computed remainder after dividing first value by the second.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of modulus operation.</returns>
    public static uint operator %(uint value1, UInt24 value2)
    {
        return value1 % (uint)value2;
    }

    /// <summary>
    /// Returns computed remainder after dividing first value by the second.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of modulus operation.</returns>
    public static uint operator %(UInt24 value1, uint value2)
    {
        return (uint)value1 % value2;
    }

    /// <summary>
    /// Returns computed sum of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="UInt24"/> value as result of addition operation.</returns>
    public static UInt24 operator +(UInt24 value1, UInt24 value2)
    {
        return (UInt24)((uint)value1 + (uint)value2);
    }

    /// <summary>
    /// Returns computed sum of values.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of addition operation.</returns>
    public static uint operator +(uint value1, UInt24 value2)
    {
        return value1 + (uint)value2;
    }

    /// <summary>
    /// Returns computed sum of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of addition operation.</returns>
    public static uint operator +(UInt24 value1, uint value2)
    {
        return (uint)value1 + value2;
    }

    /// <summary>
    /// Returns computed difference of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="UInt24"/> value as result of subtraction operation.</returns>
    public static UInt24 operator -(UInt24 value1, UInt24 value2)
    {
        return (UInt24)((uint)value1 - (uint)value2);
    }

    /// <summary>
    /// Returns computed difference of values.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of subtraction operation.</returns>
    public static uint operator -(uint value1, UInt24 value2)
    {
        return value1 - (uint)value2;
    }

    /// <summary>
    /// Returns computed difference of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of subtraction operation.</returns>
    public static uint operator -(UInt24 value1, uint value2)
    {
        return (uint)value1 - value2;
    }

    /// <summary>
    /// Returns incremented value.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns><see cref="UInt24"/> result of increment.</returns>
    public static UInt24 operator ++(UInt24 value)
    {
        return value + 1;
    }

    /// <summary>
    /// Returns decremented value.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns><see cref="UInt24"/> result of decrement.</returns>
    public static UInt24 operator --(UInt24 value)
    {
        return value - 1;
    }

    /// <summary>
    /// Returns computed product of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="UInt24"/> value as result of multiplication operation.</returns>
    public static UInt24 operator *(UInt24 value1, UInt24 value2)
    {
        return (UInt24)((uint)value1 * (uint)value2);
    }

    /// <summary>
    /// Returns computed product of values.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of multiplication operation.</returns>
    public static uint operator *(uint value1, UInt24 value2)
    {
        return value1 * (uint)value2;
    }

    /// <summary>
    /// Returns computed product of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of multiplication operation.</returns>
    public static uint operator *(UInt24 value1, uint value2)
    {
        return (uint)value1 * value2;
    }

    // Integer division operators

    /// <summary>
    /// Returns computed division of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="UInt24"/> value as result of division operation.</returns>
    public static UInt24 operator /(UInt24 value1, UInt24 value2)
    {
        return (UInt24)((uint)value1 / (uint)value2);
    }

    /// <summary>
    /// Returns computed division of values.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of division operation.</returns>
    public static uint operator /(uint value1, UInt24 value2)
    {
        return value1 / (uint)value2;
    }

    /// <summary>
    /// Returns computed division of values.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="uint"/> value as result of division operation.</returns>
    public static uint operator /(UInt24 value1, uint value2)
    {
        return (uint)value1 / value2;
    }

    //// Standard division operators
    //public static double operator /(UInt24 value1, UInt24 value2)
    //{
    //    return ((double)value1 / (double)value2);
    //}

    //public static double operator /(uint value1, UInt24 value2)
    //{
    //    return ((double)value1 / (double)value2);
    //}

    //public static double operator /(UInt24 value1, uint value2)
    //{
    //    return ((double)value1 / (double)value2);
    //}

    // C# doesn't expose an exponent operator but some other .NET languages do,
    // so we expose the operator via its native special IL function name

    /// <summary>
    /// Returns result of first value raised to power of second value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="double"/> value as result of operation.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced), SpecialName]
    public static double op_Exponent(UInt24 value1, UInt24 value2)
    {
        return Math.Pow(value1, value2);
    }

    /// <summary>
    /// Returns result of first value raised to power of second value.
    /// </summary>
    /// <param name="value1"><see cref="uint"/> left hand operand.</param>
    /// <param name="value2"><see cref="UInt24"/> right hand operand.</param>
    /// <returns><see cref="double"/> value as result of operation.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced), SpecialName]
    public static double op_Exponent(int value1, UInt24 value2)
    {
        return Math.Pow(value1, value2);
    }

    /// <summary>
    /// Returns result of first value raised to power of second value.
    /// </summary>
    /// <param name="value1"><see cref="UInt24"/> left hand operand.</param>
    /// <param name="value2"><see cref="uint"/> right hand operand.</param>
    /// <returns><see cref="double"/> value as result of operation.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced), SpecialName]
    public static double op_Exponent(UInt24 value1, int value2)
    {
        return Math.Pow(value1, value2);
    }

    #endregion

    #endregion

    #region [ Static ]

    /// <summary>
    /// Represents the largest possible value of an Int24. This field is constant.
    /// </summary>
    public static readonly UInt24 MaxValue = (UInt24)MaxValue32;

    /// <summary>
    /// Represents the smallest possible value of an Int24. This field is constant.
    /// </summary>
    public static readonly UInt24 MinValue = (UInt24)MinValue32;

    /// <summary>Returns the specified UInt24 value as an array of three bytes.</summary>
    /// <param name="value">UInt24 value to convert to bytes.</param>
    /// <returns>An array of bytes with length 3.</returns>
    /// <remarks>
    /// <para>You can use this function in-lieu of a System.BitConverter.GetBytes(UInt24) function.</para>
    /// <para>Bytes will be returned in endian order of currently executing process architecture (little-endian on Intel platforms).</para>
    /// </remarks>
    public static byte[] GetBytes(UInt24 value)
    {
        // We use a 32-bit integer to store 24-bit integer internally
        byte[] data = new byte[3];
        uint valueInt = value;

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

    /// <summary>Returns a 24-bit unsigned integer from three bytes at a specified position in a byte array.</summary>
    /// <param name="value">An array of bytes.</param>
    /// <param name="startIndex">The starting position within value.</param>
    /// <returns>A 24-bit unsigned integer formed by three bytes beginning at startIndex.</returns>
    /// <remarks>
    /// <para>You can use this function in-lieu of a System.BitConverter.ToUInt24 function.</para>
    /// <para>Bytes endian order assumed to match that of currently executing process architecture (little-endian on Intel platforms).</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is greater than <paramref name="value"/> length.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> length from <paramref name="startIndex"/> is too small to represent an <see cref="UInt24"/>.</exception>
    public static UInt24 GetValue(byte[] value, int startIndex)
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
        return (UInt24)ApplyBitMask((uint)valueInt);
    }

    private static void ValidateNumericRange(uint value)
    {
        if (value > MaxValue32)
            throw new OverflowException($"Value of {value} will not fit in a 24-bit unsigned integer");
    }

    private static uint ApplyBitMask(uint value)
    {
        // For unsigned values, all we do is clear all the high bits (keeps 32-bit unsigned number in 24-bit unsigned range)...
        return value & ~BitMask;
    }

    #endregion
}