
//******************************************************************************************************
//  ArrayExtensions.cs - Gbtc
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
//  09/19/2008 - J. Ritchie Carroll
//       Generated original version of source code.
//  12/03/2008 - J. Ritchie Carroll
//       Added "Combine" and "IndexOfSequence" overloaded extensions.
//  02/13/2009 - Josh L. Patterson
//       Edited Code Comments.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/31/2009 - Andrew K. Hill
//       Modified the following methods per unit testing:
//       BlockCopy(T[], int, int)
//       Combine(T[], T[])
//       Combine(T[], int, int, T[], int, int)
//       Combine(T[][])
//       IndexOfSequence(T[], T[])
//       IndexOfSequence(T[], T[], int)
//       IndexOfSequence(T[], T[], int, int)
//  11/22/2011 - J. Ritchie Carroll
//       Added common case array parameter validation extensions
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//  11/02/2023 - AJ Stadlin
//       Added Extensions:
//       CountOfSequence(T[], T[])
//       CountOfSequence(T[], T[], int)
//       CountOfSequence(T[], T[], int, int)
//
//******************************************************************************************************

//******************************************************************************************************
//  BlockAllocatedMemoryStream.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
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
//  06/14/2013 - J. Ritchie Carroll
//       Adapted from the "MemoryTributary" class written by Sebastian Friston:
//          Source Code: http://memorytributary.codeplex.com/
//          Article: http://www.codeproject.com/Articles/348590/A-replacement-for-MemoryStream
//  11/21/2016 - Steven E. Chisholm
//       A complete refresh of BlockAllocatedMemoryStream and how it works.
//
//******************************************************************************************************

//******************************************************************************************************
//  BufferPool.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
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
//  11/17/2016 - Steven E. Chisholm
//       Generated original version of source code. 
//  12/26/2019 - J. Ritchie Carroll
//       Simplified DynamicObjectPool as an internal resource renaming to BufferPool.
//
//******************************************************************************************************


using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System;

public static class ArrayExtensions
{
    /// <summary>
    /// Zero the given buffer in a way that will not be optimized away.
    /// </summary>
    /// <param name="buffer">Buffer to zero.</param>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static void Zero<T>(this T[] buffer)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        // Zero buffer
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = default!;
    }

    /// <summary>
    /// Validates that the specified <paramref name="startIndex"/> and <paramref name="length"/> are valid within the given <paramref name="array"/>.
    /// </summary>
    /// <param name="array">Array to validate.</param>
    /// <param name="startIndex">0-based start index into the <paramref name="array"/>.</param>
    /// <param name="length">Valid number of items within <paramref name="array"/> from <paramref name="startIndex"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startIndex"/> or <paramref name="length"/> is less than 0 -or- 
    /// <paramref name="startIndex"/> and <paramref name="length"/> will exceed <paramref name="array"/> length.
    /// </exception>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateParameters<T>(this T[]? array, int startIndex, int length)
    {
        if (array is null || startIndex < 0 || length < 0 || startIndex + length > array.Length)
            RaiseValidationError(array, startIndex, length);
    }

    // This method will raise the actual error - this is needed since .NET will not inline anything that might throw an exception
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RaiseValidationError<T>(T[]? array, int startIndex, int length)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "cannot be negative");

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "cannot be negative");

        if (startIndex + length > array.Length)
            throw new ArgumentOutOfRangeException(nameof(length), $"startIndex of {startIndex} and length of {length} will exceed array size of {array.Length}");
    }

    /// <summary>
    /// Returns a copy of the specified portion of the <paramref name="array"/> array.
    /// </summary>
    /// <param name="array">Source array.</param>
    /// <param name="startIndex">Offset into <paramref name="array"/> array.</param>
    /// <param name="length">Length of <paramref name="array"/> array to copy at <paramref name="startIndex"/> offset.</param>
    /// <returns>An array of data copied from the specified portion of the source array.</returns>
    /// <remarks>
    /// <para>
    /// Returned array will be extended as needed to make it the specified <paramref name="length"/>, but
    /// it will never be less than the source array length - <paramref name="startIndex"/>.
    /// </para>
    /// <para>
    /// If an existing array of primitives is already available, using the <see cref="Buffer.BlockCopy"/> directly
    /// instead of this extension method may be optimal since this method always allocates a new return array.
    /// Unlike <see cref="Buffer.BlockCopy"/>, however, this function also works with non-primitive types.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startIndex"/> is outside the range of valid indexes for the source array -or-
    /// <paramref name="length"/> is less than 0.
    /// </exception>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static T[] BlockCopy<T>(this T[] array, int startIndex, int length)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "cannot be negative");

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "cannot be negative");

        if (startIndex >= array.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "not a valid index into the array");

        length = array.Length - startIndex < length ? array.Length - startIndex : length;
        T[] copiedBytes = new T[length];

        if (typeof(T).IsPrimitive)
            Buffer.BlockCopy(array, startIndex, copiedBytes, 0, length);
        else
            Array.Copy(array, startIndex, copiedBytes, 0, length);

        return copiedBytes;
    }

    /// <summary>
    /// Combines arrays together into a single array.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="other">Other array to combine to <paramref name="source"/> array.</param>
    /// <returns>Combined arrays.</returns>
    /// <remarks>
    /// <para>
    /// Only use this function if you need a copy of the combined arrays, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined arrays.
    /// </para>
    /// <para>
    /// This function can easily throw an out of memory exception if there is not enough
    /// contiguous memory to create an array sized with the combined lengths.
    /// </para>
    /// </remarks>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static T[] Combine<T>(this T[] source, T[] other)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (other is null)
            throw new ArgumentNullException(nameof(other));

        return source.Combine(0, source.Length, other, 0, other.Length);
    }

    /// <summary>
    /// Combines specified portions of arrays together into a single array.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="sourceOffset">Offset into <paramref name="source"/> array to begin copy.</param>
    /// <param name="sourceCount">Number of bytes to copy from <paramref name="source"/> array.</param>
    /// <param name="other">Other array to combine to <paramref name="source"/> array.</param>
    /// <param name="otherOffset">Offset into <paramref name="other"/> array to begin copy.</param>
    /// <param name="otherCount">Number of bytes to copy from <paramref name="other"/> array.</param>
    /// <returns>Combined specified portions of both arrays.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="sourceOffset"/> or <paramref name="otherOffset"/> is outside the range of valid indexes for the associated array -or-
    /// <paramref name="sourceCount"/> or <paramref name="otherCount"/> is less than 0 -or- 
    /// <paramref name="sourceOffset"/> or <paramref name="otherOffset"/>, 
    /// and <paramref name="sourceCount"/> or <paramref name="otherCount"/> do not specify a valid section in the associated array.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Only use this function if you need a copy of the combined arrays, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined arrays.
    /// </para>
    /// <para>
    /// This function can easily throw an out of memory exception if there is not enough
    /// contiguous memory to create an array sized with the combined lengths.
    /// </para>
    /// </remarks>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static T[] Combine<T>(this T[] source, int sourceOffset, int sourceCount, T[] other, int otherOffset, int otherCount)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (sourceOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceOffset), "cannot be negative");

        if (otherOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(otherOffset), "cannot be negative");

        if (sourceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceCount), "cannot be negative");

        if (otherCount < 0)
            throw new ArgumentOutOfRangeException(nameof(otherCount), "cannot be negative");

        if (sourceOffset >= source.Length)
            throw new ArgumentOutOfRangeException(nameof(sourceOffset), "not a valid index into source array");

        if (otherOffset >= other.Length)
            throw new ArgumentOutOfRangeException(nameof(otherOffset), "not a valid index into other array");

        if (sourceOffset + sourceCount > source.Length)
            throw new ArgumentOutOfRangeException(nameof(sourceCount), "exceeds source array size");

        if (otherOffset + otherCount > other.Length)
            throw new ArgumentOutOfRangeException(nameof(otherCount), "exceeds other array size");

        // Overflow is possible, but unlikely.  Therefore, this is omitted for performance
        // if ((int.MaxValue - sourceCount - otherCount) < 0)
        //    throw new ArgumentOutOfRangeException("sourceCount + otherCount", "exceeds maximum array size");

        // Combine arrays together as a single image
        T[] combinedBuffer = new T[sourceCount + otherCount];

        if (typeof(T).IsPrimitive)
        {
            Buffer.BlockCopy(source, sourceOffset, combinedBuffer, 0, sourceCount);
            Buffer.BlockCopy(other, otherOffset, combinedBuffer, sourceCount, otherCount);
        }
        else
        {
            Array.Copy(source, sourceOffset, combinedBuffer, 0, sourceCount);
            Array.Copy(other, otherOffset, combinedBuffer, sourceCount, otherCount);
        }

        return combinedBuffer;
    }

    /// <summary>
    /// Combines arrays together into a single array.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="other1">First array to combine to <paramref name="source"/> array.</param>
    /// <param name="other2">Second array to combine to <paramref name="source"/> array.</param>
    /// <returns>Combined arrays.</returns>
    /// <remarks>
    /// <para>
    /// Only use this function if you need a copy of the combined arrays, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined arrays.
    /// </para>
    /// <para>
    /// This function can easily throw an out of memory exception if there is not enough
    /// contiguous memory to create an array sized with the combined lengths.
    /// </para>
    /// </remarks>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static T[] Combine<T>(this T[] source, T[] other1, T[] other2)
    {
        return new[] { source, other1, other2 }.Combine();
    }

    /// <summary>
    /// Combines arrays together into a single array.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="other1">First array to combine to <paramref name="source"/> array.</param>
    /// <param name="other2">Second array to combine to <paramref name="source"/> array.</param>
    /// <param name="other3">Third array to combine to <paramref name="source"/> array.</param>
    /// <returns>Combined arrays.</returns>
    /// <remarks>
    /// <para>
    /// Only use this function if you need a copy of the combined arrays, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined arrays.
    /// </para>
    /// <para>
    /// This function can easily throw an out of memory exception if there is not enough
    /// contiguous memory to create an array sized with the combined lengths.
    /// </para>
    /// </remarks>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static T[] Combine<T>(this T[] source, T[] other1, T[] other2, T[] other3)
    {
        return new[] { source, other1, other2, other3 }.Combine();
    }

    /// <summary>
    /// Combines arrays together into a single array.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="other1">First array to combine to <paramref name="source"/> array.</param>
    /// <param name="other2">Second array to combine to <paramref name="source"/> array.</param>
    /// <param name="other3">Third array to combine to <paramref name="source"/> array.</param>
    /// <param name="other4">Fourth array to combine to <paramref name="source"/> array.</param>
    /// <returns>Combined arrays.</returns>
    /// <remarks>
    /// <para>
    /// Only use this function if you need a copy of the combined arrays, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined arrays.
    /// </para>
    /// <para>
    /// This function can easily throw an out of memory exception if there is not enough
    /// contiguous memory to create an array sized with the combined lengths.
    /// </para>
    /// </remarks>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static T[] Combine<T>(this T[] source, T[] other1, T[] other2, T[] other3, T[] other4)
    {
        return new[] { source, other1, other2, other3, other4 }.Combine();
    }

    /// <summary>
    /// Combines array of arrays together into a single array.
    /// </summary>
    /// <param name="arrays">Array of arrays to combine.</param>
    /// <returns>Combined arrays.</returns>
    /// <remarks>
    /// <para>
    /// Only use this function if you need a copy of the combined arrays, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined arrays.
    /// </para>
    /// <para>
    /// This function can easily throw an out of memory exception if there is not enough
    /// contiguous memory to create an array sized with the combined lengths.
    /// </para>
    /// </remarks>
    /// <typeparam name="T"><see cref="Type"/> of arrays.</typeparam>
    public static T[] Combine<T>(this T[][] arrays)
    {
        if (arrays is null)
            throw new ArgumentNullException(nameof(arrays));

        int size = arrays.Sum(array => array.Length);
        int offset = 0;

        // Combine arrays together as a single image
        T[] combinedBuffer = new T[size];

        for (int i = 0; i < arrays.Length; i++)
        {
            if (arrays[i] is null)
                throw new ArgumentNullException($"arrays[{i}]");

            int length = arrays[i].Length;

            if (length == 0)
                continue;

            Array.Copy(arrays[i], 0, combinedBuffer, offset, length);

            offset += length;
        }

        return combinedBuffer;
    }

    /// <summary>
    /// Searches for the specified <paramref name="sequenceToFind"/> and returns the index of the first occurrence within the <paramref name="array"/>.
    /// </summary>
    /// <param name="array">Array to search.</param>
    /// <param name="sequenceToFind">Sequence of items to search for.</param>
    /// <returns>The zero-based index of the first occurrence of the <paramref name="sequenceToFind"/> in the <paramref name="array"/>, if found; otherwise, -1.</returns>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static int IndexOfSequence<T>(this T[] array, T[] sequenceToFind) where T : IComparable<T>
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (sequenceToFind is null)
            throw new ArgumentNullException(nameof(sequenceToFind));

        return array.IndexOfSequence(sequenceToFind, 0, array.Length);
    }

    /// <summary>
    /// Searches for the specified <paramref name="sequenceToFind"/> and returns the index of the first occurrence within the range of elements in the <paramref name="array"/>
    /// that starts at the specified index.
    /// </summary>
    /// <param name="array">Array to search.</param>
    /// <param name="sequenceToFind">Sequence of items to search for.</param>
    /// <param name="startIndex">Start index in the <paramref name="array"/> to start searching.</param>
    /// <returns>The zero-based index of the first occurrence of the <paramref name="sequenceToFind"/> in the <paramref name="array"/>, if found; otherwise, -1.</returns>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static int IndexOfSequence<T>(this T[] array, T[] sequenceToFind, int startIndex) where T : IComparable<T>
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (sequenceToFind is null)
            throw new ArgumentNullException(nameof(sequenceToFind));

        return array.IndexOfSequence(sequenceToFind, startIndex, array.Length - startIndex);
    }

    /// <summary>
    /// Searches for the specified <paramref name="sequenceToFind"/> and returns the index of the first occurrence within the range of elements in the <paramref name="array"/>
    /// that starts at the specified index and contains the specified number of elements.
    /// </summary>
    /// <param name="array">Array to search.</param>
    /// <param name="sequenceToFind">Sequence of items to search for.</param>
    /// <param name="startIndex">Start index in the <paramref name="array"/> to start searching.</param>
    /// <param name="length">Number of bytes in the <paramref name="array"/> to search through.</param>
    /// <returns>The zero-based index of the first occurrence of the <paramref name="sequenceToFind"/> in the <paramref name="array"/>, if found; otherwise, -1.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="sequenceToFind"/> is null or has zero length.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startIndex"/> is outside the range of valid indexes for the source array -or-
    /// <paramref name="length"/> is less than 0.
    /// </exception>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static int IndexOfSequence<T>(this T[] array, T[] sequenceToFind, int startIndex, int length) where T : IComparable<T>
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (sequenceToFind is null || sequenceToFind.Length == 0)
            throw new ArgumentNullException(nameof(sequenceToFind));

        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "cannot be negative");

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "cannot be negative");

        if (startIndex >= array.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "not a valid index into source array");

        if (startIndex + length > array.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "exceeds array size");

        // Overflow is possible, but unlikely.  Therefore, this is omitted for performance
        // if ((int.MaxValue - startIndex - length) < 0)
        //    throw new ArgumentOutOfRangeException("startIndex + length", "exceeds maximum array size");            

        // Search for first item in the sequence, if this doesn't exist then sequence doesn't exist
        int index = Array.IndexOf(array, sequenceToFind[0], startIndex, length);

        if (sequenceToFind.Length <= 1)
            return index;

        bool foundSequence = false;

        while (index > -1 && !foundSequence)
        {
            // See if next bytes in sequence match
            for (int x = 1; x < sequenceToFind.Length; x++)
            {
                // Make sure there's enough array remaining to accommodate this item
                if (index + x < startIndex + length)
                {
                    // If sequence doesn't match, search for next first-item
                    if (array[index + x].CompareTo(sequenceToFind[x]) != 0)
                    {
                        index = Array.IndexOf(array, sequenceToFind[0], index + 1, startIndex + length - (index + 1));
                        break;
                    }

                    // If each item to find matched, we found the sequence
                    foundSequence = x == sequenceToFind.Length - 1;
                }
                else
                {
                    // Ran out of array, return -1
                    index = -1;
                }
            }
        }

        return index;
    }

    /// <summary>
    /// Searches for the specified <paramref name="sequenceToCount"/> and returns the occurrence count within the <paramref name="array"/>.
    /// </summary>
    /// <param name="array">Array to search.</param>
    /// <param name="sequenceToCount">Sequence of items to search for.</param>
    /// <returns>The occurrence count of the <paramref name="sequenceToCount"/> in the <paramref name="array"/>, if found; otherwise, -1.</returns>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static int CountOfSequence<T>(this T[] array, T[] sequenceToCount) where T : IComparable<T>
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (sequenceToCount is null)
            throw new ArgumentNullException(nameof(sequenceToCount));

        return array.CountOfSequence(sequenceToCount, 0, array.Length);
    }

    /// <summary>
    /// Searches for the specified <paramref name="sequenceToCount"/> and returns the occurence count within the range of elements in the <paramref name="array"/>
    /// that starts at the specified index.
    /// </summary>
    /// <param name="array">Array to search.</param>
    /// <param name="sequenceToCount">Sequence of items to search for.</param>
    /// <param name="startIndex">Start index in the <paramref name="array"/> to start searching.</param>
    /// <returns>The occurrence count of the <paramref name="sequenceToCount"/> in the <paramref name="array"/>, if found; otherwise, -1.</returns>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static int CountOfSequence<T>(this T[] array, T[] sequenceToCount, int startIndex) where T : IComparable<T>
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (sequenceToCount is null)
            throw new ArgumentNullException(nameof(sequenceToCount));

        return array.CountOfSequence(sequenceToCount, startIndex, array.Length - startIndex);
    }

    /// <summary>
    /// Searches for the specified <paramref name="sequenceToCount"/> and returns the occurrence count within the range of elements in the <paramref name="array"/>
    /// that starts at the specified index and contains the specified number of elements.
    /// </summary>
    /// <param name="array">Array to search.</param>
    /// <param name="sequenceToCount">Sequence of items to search for.</param>
    /// <param name="startIndex">Start index in the <paramref name="array"/> to start searching.</param>
    /// <param name="searchLength">Number of bytes in the <paramref name="array"/> to search through.</param>
    /// <returns>The occurrence count of the <paramref name="sequenceToCount"/> in the <paramref name="array"/>, if found; otherwise, -1.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="sequenceToCount"/> is null or has zero length.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startIndex"/> is outside the range of valid indexes for the source array -or-
    /// <paramref name="searchLength"/> is less than 0.
    /// </exception>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static int CountOfSequence<T>(this T[] array, T[] sequenceToCount, int startIndex, int searchLength) where T : IComparable<T>
    {
        if (array is null || array.Length == 0)
            throw new ArgumentNullException(nameof(array));

        if (sequenceToCount is null || sequenceToCount.Length == 0)
            throw new ArgumentNullException(nameof(sequenceToCount));

        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "cannot be negative");

        if (startIndex >= array.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "not a valid index into source array");

        if (searchLength < 0)
            throw new ArgumentOutOfRangeException(nameof(searchLength), "cannot be negative");

        if (startIndex + searchLength > array.Length)
            throw new ArgumentOutOfRangeException(nameof(searchLength), "exceeds array size");

        // Overflow is possible, but unlikely.  Therefore, this is omitted for performance
        // if ((int.MaxValue - startIndex - length) < 0)
        //    throw new ArgumentOutOfRangeException("startIndex + length", "exceeds maximum array size");

        // Search for first item in the sequence, if this doesn't exist then sequence doesn't exist
        int index = Array.IndexOf(array, sequenceToCount[0], startIndex, searchLength);

        if (index < 0)
            return 0;

        // Occurrences counter
        int foundCount = 0;

        // Search when the first array element is found, and the sequence can fit in the search range
        bool searching = sequenceToCount.Length <= startIndex + searchLength - index;

        while (searching)
        {
            // See if bytes in sequence match
            for (int x = 0; x < sequenceToCount.Length; x++)
            {
                // If sequence doesn't match, search for next item
                if (array[index + x].CompareTo(sequenceToCount[x]) != 0)
                {
                    index++;
                    index = Array.IndexOf(array, sequenceToCount[0], index, startIndex + searchLength - index);
                    break;
                }

                // When each item to find matched, we found the sequence
                if (x == sequenceToCount.Length - 1)
                {
                    foundCount++;
                    index++;
                    index = Array.IndexOf(array, sequenceToCount[0], index, startIndex + searchLength - index);
                }
            }

            // Continue searching if the array remaining can accommodate the sequence to find
            searching = index > -1 && sequenceToCount.Length <= startIndex + searchLength - index;
        }

        return foundCount;
    }

    /// <summary>Returns comparison results of two binary arrays.</summary>
    /// <param name="source">Source array.</param>
    /// <param name="other">Other array to compare to <paramref name="source"/> array.</param>
    /// <remarks>
    /// Note that if both arrays are <c>null</c> the arrays will be considered equal.
    /// If one array is <c>null</c> and the other array is not <c>null</c>, the non-null array will be considered larger.
    /// If the array lengths are not equal, the array with the larger length will be considered larger.
    /// If the array lengths are equal, the arrays will be compared based on content.
    /// </remarks>
    /// <returns>
    /// <para>
    /// A signed integer that indicates the relative comparison of <paramref name="source"/> array and <paramref name="other"/> array.
    /// </para>
    /// <para>
    /// <list type="table">
    ///     <listheader>
    ///         <term>Return Value</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Less than zero</term>
    ///         <description>Source array is less than other array.</description>
    ///     </item>
    ///     <item>
    ///         <term>Zero</term>
    ///         <description>Source array is equal to other array.</description>
    ///     </item>
    ///     <item>
    ///         <term>Greater than zero</term>
    ///         <description>Source array is greater than other array.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </returns>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static int CompareTo<T>(this T[]? source, T[]? other) where T : IComparable<T>
    {
        // If both arrays are assumed equal if both are nothing
        if (source is null && other is null)
            return 0;

        // If other array has data and source array is nothing, other array is assumed larger
        if (source is null)
            return -1;

        // If source array has data and other array is nothing, source array is assumed larger
        if (other is null)
            return 1;

        int length1 = source.Length;
        int length2 = other.Length;

        // If array lengths are unequal, array with the largest number of elements is assumed to be largest
        if (length1 != length2)
            return length1.CompareTo(length2);

        int comparison = 0;

        // Compares elements of arrays that are of equal size.
        for (int x = 0; x < length1; x++)
        {
            comparison = source[x].CompareTo(other[x]);

            if (comparison != 0)
                break;
        }

        return comparison;
    }

    /// <summary>
    /// Returns comparison results of two binary arrays.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="sourceOffset">Offset into <paramref name="source"/> array to begin compare.</param>
    /// <param name="other">Other array to compare to <paramref name="source"/> array.</param>
    /// <param name="otherOffset">Offset into <paramref name="other"/> array to begin compare.</param>
    /// <param name="count">Number of bytes to compare in both arrays.</param>
    /// <remarks>
    /// Note that if both arrays are <c>null</c> the arrays will be considered equal.
    /// If one array is <c>null</c> and the other array is not <c>null</c>, the non-null array will be considered larger.
    /// </remarks>
    /// <returns>
    /// <para>
    /// A signed integer that indicates the relative comparison of <paramref name="source"/> array and <paramref name="other"/> array.
    /// </para>
    /// <para>
    /// <list type="table">
    ///     <listheader>
    ///         <term>Return Value</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Less than zero</term>
    ///         <description>Source array is less than other array.</description>
    ///     </item>
    ///     <item>
    ///         <term>Zero</term>
    ///         <description>Source array is equal to other array.</description>
    ///     </item>
    ///     <item>
    ///         <term>Greater than zero</term>
    ///         <description>Source array is greater than other array.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="sourceOffset"/> or <paramref name="otherOffset"/> is outside the range of valid indexes for the associated array -or-
    /// <paramref name="count"/> is less than 0 -or- 
    /// <paramref name="sourceOffset"/> or <paramref name="otherOffset"/> and <paramref name="count"/> do not specify a valid section in the associated array.
    /// </exception>
    /// <typeparam name="T"><see cref="Type"/> of array.</typeparam>
    public static int CompareTo<T>(this T[]? source, int sourceOffset, T[]? other, int otherOffset, int count) where T : IComparable<T>
    {
        // If both arrays are assumed equal if both are nothing
        if (source is null && other is null)
            return 0;

        // If other array has data and source array is nothing, other array is assumed larger
        if (source is null)
            return -1;

        // If source array has data and other array is nothing, source array is assumed larger
        if (other is null)
            return 1;

        if (sourceOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceOffset), "cannot be negative");

        if (otherOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(otherOffset), "cannot be negative");

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "cannot be negative");

        if (sourceOffset >= source.Length)
            throw new ArgumentOutOfRangeException(nameof(sourceOffset), "not a valid index into source array");

        if (otherOffset >= other.Length)
            throw new ArgumentOutOfRangeException(nameof(otherOffset), "not a valid index into other array");

        if (sourceOffset + count > source.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "exceeds source array size");

        if (otherOffset + count > other.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "exceeds other array size");

        // Overflow is possible, but unlikely.  Therefore, this is omitted for performance
        // if ((int.MaxValue - sourceOffset - count) < 0)
        //    throw new ArgumentOutOfRangeException("sourceOffset + count", "exceeds maximum array size");

        // Overflow is possible, but unlikely.  Therefore, this is omitted for performance
        // if ((int.MaxValue - otherOffset - count) < 0)
        //    throw new ArgumentOutOfRangeException("sourceOffset + count", "exceeds maximum array size");

        int comparison = 0;

        // Compares elements of arrays that are of equal size.
        for (int x = 0; x < count; x++)
        {
            comparison = source[sourceOffset + x].CompareTo(other[otherOffset + x]);

            if (comparison != 0)
                break;
        }

        return comparison;
    }

    // Handling byte arrays as a special case for combining multiple buffers since this can
    // use a block allocated memory stream

    /// <summary>
    /// Combines buffers together as a single image.
    /// </summary>
    /// <param name="source">Source buffer.</param>
    /// <param name="other1">First buffer to combine to <paramref name="source"/> buffer.</param>
    /// <param name="other2">Second buffer to combine to <paramref name="source"/> buffer.</param>
    /// <returns>Combined buffers.</returns>
    /// <exception cref="InvalidOperationException">Cannot create a byte array with more than 2,147,483,591 elements.</exception>
    /// <remarks>
    /// Only use this function if you need a copy of the combined buffers, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined buffers.
    /// </remarks>
    public static byte[] Combine(this byte[] source, byte[] other1, byte[] other2)
    {
        return new[] { source, other1, other2 }.Combine();
    }

    /// <summary>
    /// Combines buffers together as a single image.
    /// </summary>
    /// <param name="source">Source buffer.</param>
    /// <param name="other1">First buffer to combine to <paramref name="source"/> buffer.</param>
    /// <param name="other2">Second buffer to combine to <paramref name="source"/> buffer.</param>
    /// <param name="other3">Third buffer to combine to <paramref name="source"/> buffer.</param>
    /// <returns>Combined buffers.</returns>
    /// <exception cref="InvalidOperationException">Cannot create a byte array with more than 2,147,483,591 elements.</exception>
    /// <remarks>
    /// Only use this function if you need a copy of the combined buffers, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined buffers.
    /// </remarks>
    public static byte[] Combine(this byte[] source, byte[] other1, byte[] other2, byte[] other3)
    {
        return new[] { source, other1, other2, other3 }.Combine();
    }

    /// <summary>
    /// Combines buffers together as a single image.
    /// </summary>
    /// <param name="source">Source buffer.</param>
    /// <param name="other1">First buffer to combine to <paramref name="source"/> buffer.</param>
    /// <param name="other2">Second buffer to combine to <paramref name="source"/> buffer.</param>
    /// <param name="other3">Third buffer to combine to <paramref name="source"/> buffer.</param>
    /// <param name="other4">Fourth buffer to combine to <paramref name="source"/> buffer.</param>
    /// <returns>Combined buffers.</returns>
    /// <exception cref="InvalidOperationException">Cannot create a byte array with more than 2,147,483,591 elements.</exception>
    /// <remarks>
    /// Only use this function if you need a copy of the combined buffers, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined buffers.
    /// </remarks>
    public static byte[] Combine(this byte[] source, byte[] other1, byte[] other2, byte[] other3, byte[] other4)
    {
        return new[] { source, other1, other2, other3, other4 }.Combine();
    }

    /// <summary>
    /// Combines an array of buffers together as a single image.
    /// </summary>
    /// <param name="buffers">Array of byte buffers.</param>
    /// <returns>Combined buffers.</returns>
    /// <exception cref="InvalidOperationException">Cannot create a byte array with more than 2,147,483,591 elements.</exception>
    /// <remarks>
    /// Only use this function if you need a copy of the combined buffers, it will be optimal
    /// to use the Linq function <see cref="Enumerable.Concat{T}"/> if you simply need to
    /// iterate over the combined buffers.
    /// </remarks>
    public static byte[] Combine(this byte[][] buffers)
    {
        if (buffers is null)
            throw new ArgumentNullException(nameof(buffers));

        using BlockAllocatedMemoryStream combinedBuffer = new();

        // Combine all currently queued buffers
        for (int x = 0; x < buffers.Length; x++)
        {
            if (buffers[x] is null)
                throw new ArgumentNullException($"buffers[{x}]");

            combinedBuffer.Write(buffers[x], 0, buffers[x].Length);
        }

        // return combined data buffers
        return combinedBuffer.ToArray();
    }

    /// <summary>
    /// Reads a structure from a byte array.
    /// </summary>
    /// <typeparam name="T">Type of structure to read.</typeparam>
    /// <param name="bytes">Bytes containing structure.</param>
    /// <returns>A structure from <paramref name="bytes"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T? ReadStructure<T>(this byte[] bytes) where T : struct
    {
        T? structure;

        fixed (byte* ptrToBytes = bytes)
            structure = (T?)Marshal.PtrToStructure(new IntPtr(ptrToBytes), typeof(T));

        return structure;
    }

    /// <summary>
    /// Reads a structure from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <typeparam name="T">Type of structure to read.</typeparam>
    /// <param name="reader"><see cref="BinaryReader"/> positioned at desired structure.</param>
    /// <returns>A structure read from <see cref="BinaryReader"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? ReadStructure<T>(this BinaryReader reader) where T : struct =>
        reader.ReadBytes(Marshal.SizeOf(typeof(T))).ReadStructure<T>();


    #region [ Block Allocated Memory Stream ]

    /// <summary>
    /// Defines a stream whose backing store is memory. Externally this class operates similar to a <see cref="MemoryStream"/>,
    /// internally it uses dynamically allocated buffer blocks instead of one large contiguous array of data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="BlockAllocatedMemoryStream"/> has two primary benefits over a normal <see cref="MemoryStream"/>, first, the
    /// allocation of a large contiguous array of data in <see cref="MemoryStream"/> can fail when the requested amount of contiguous
    /// memory is unavailable - the <see cref="BlockAllocatedMemoryStream"/> prevents this; second, a <see cref="MemoryStream"/> will
    /// constantly reallocate the buffer size as the stream grows and shrinks and then copy all the data from the old buffer to the
    /// new - the <see cref="BlockAllocatedMemoryStream"/> maintains its blocks over its life cycle, unless manually cleared, thus
    /// eliminating unnecessary allocations and garbage collections when growing and reusing a stream.
    /// </para>
    /// <para>
    /// Important: Unlike <see cref="MemoryStream"/>, the <see cref="BlockAllocatedMemoryStream"/> will not use a user provided buffer
    /// as its backing buffer. Any user provided buffers used to instantiate the class will be copied into internally managed reusable
    /// memory buffers. Subsequently, the <see cref="BlockAllocatedMemoryStream"/> does not support the notion of a non-expandable
    /// stream. If you are using a <see cref="MemoryStream"/> with your own buffer, the <see cref="BlockAllocatedMemoryStream"/> will
    /// not provide any immediate benefit.
    /// </para>
    /// <para>
    /// Note that the <see cref="BlockAllocatedMemoryStream"/> will maintain all allocated blocks for stream use until the
    /// <see cref="Clear"/> method is called or the class is disposed.
    /// </para>
    /// <para>
    /// No members in the <see cref="BlockAllocatedMemoryStream"/> are guaranteed to be thread safe. Make sure any calls are
    /// synchronized when simultaneously accessed from different threads.
    /// </para>
    /// </remarks>
    public class BlockAllocatedMemoryStream : Stream
    {
        // Note: Since byte blocks are pooled, they will not be 
        //       initialized unless a Read/Write operation occurs 
        //       when m_position > m_length

        #region [ Members ]

        // Constants
        private const int BlockSize = 8 * 1024;
        private const int ShiftBits = 3 + 10;
        private const int BlockMask = BlockSize - 1;

        // Fields
        private List<byte[]> m_blocks;
        private long m_length;
        private long m_position;
        private long m_capacity;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of <see cref="BlockAllocatedMemoryStream"/>.
        /// </summary>
        public BlockAllocatedMemoryStream() => m_blocks = new List<byte[]>();

        /// <summary>
        /// Initializes a new instance of <see cref="BlockAllocatedMemoryStream"/> from specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">Initial buffer to copy into stream.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <remarks>
        /// Unlike <see cref="MemoryStream"/>, the <see cref="BlockAllocatedMemoryStream"/> will not use the provided
        /// <paramref name="buffer"/> as its backing buffer. The buffer will be copied into internally managed reusable
        /// memory buffers. Subsequently, the notion of a non-expandable stream is not supported.
        /// </remarks>
        public BlockAllocatedMemoryStream(byte[] buffer) : this(buffer, 0, buffer.Length)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockAllocatedMemoryStream"/> from specified region of <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">Initial buffer to copy into stream.</param>
        /// <param name="startIndex">0-based start index into the <paramref name="buffer"/>.</param>
        /// <param name="length">Valid number of bytes within <paramref name="buffer"/> from <paramref name="startIndex"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> or <paramref name="length"/> is less than 0 -or- 
        /// <paramref name="startIndex"/> and <paramref name="length"/> will exceed <paramref name="buffer"/> length.
        /// </exception>
        /// <remarks>
        /// Unlike <see cref="MemoryStream"/>, the <see cref="BlockAllocatedMemoryStream"/> will not use the provided
        /// <paramref name="buffer"/> as its backing buffer. The buffer will be copied into internally managed reusable
        /// memory buffers. Subsequently, the notion of a non-expandable stream is not supported.
        /// </remarks>
        public BlockAllocatedMemoryStream(byte[] buffer, int startIndex, int length) : this()
        {
            buffer.ValidateParameters(startIndex, length);
            Write(buffer, startIndex, length);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockAllocatedMemoryStream"/> for specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">Initial length of the stream.</param>
        public BlockAllocatedMemoryStream(int capacity) : this() => SetLength(capacity);

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets a value that indicates whether the <see cref="BlockAllocatedMemoryStream"/> object supports reading.
        /// </summary>
        /// <remarks>
        /// This is always <c>true</c>.
        /// </remarks>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="BlockAllocatedMemoryStream"/> object supports seeking.
        /// </summary>
        /// <remarks>
        /// This is always <c>true</c>.
        /// </remarks>
        public override bool CanSeek => true;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="BlockAllocatedMemoryStream"/> object supports writing.
        /// </summary>
        /// <remarks>
        /// This is always <c>true</c>.
        /// </remarks>
        public override bool CanWrite => true;

        /// <summary>
        /// Gets current stream length for this <see cref="BlockAllocatedMemoryStream"/> instance.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override long Length
        {
            get
            {
                if (m_disposed)
                    throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

                return m_length;
            }
        }

        /// <summary>
        /// Gets current stream position for this <see cref="BlockAllocatedMemoryStream"/> instance.
        /// </summary>
        /// <exception cref="IOException">Seeking was attempted before the beginning of the stream.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override long Position
        {
            get
            {
                if (m_disposed)
                    throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

                return m_position;
            }
            set
            {
                if (m_disposed)
                    throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

                if (value < 0L)
                    throw new IOException("Seek was attempted before the beginning of the stream.");

                m_position = value;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="BlockAllocatedMemoryStream"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; otherwise, <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            try
            {
                // Make sure buffer blocks get returned to the pool
                if (disposing)
                    Clear();
            }
            finally
            {
                m_disposed = true;          // Prevent duplicate dispose.
                base.Dispose(disposing);    // Call base class Dispose().
            }
        }

        /// <summary>
        /// Clears the entire <see cref="BlockAllocatedMemoryStream"/> contents and releases any allocated memory blocks.
        /// </summary>
        public void Clear()
        {
            m_position = 0;
            m_length = 0;
            m_capacity = 0;

            // In the event that an exception occurs, we don't want to have released blocks that are still in this memory stream.
            List<byte[]> blocks = m_blocks;

            m_blocks = new List<byte[]>();

            foreach (byte[] block in blocks)
                s_memoryBlockPool.Enqueue(block);
        }

        /// <summary>
        /// Sets the <see cref="Position"/> within the current stream to the specified value relative the <paramref name="origin"/>.
        /// </summary>
        /// <returns>
        /// The new position within the stream, calculated by combining the initial reference point and the offset.
        /// </returns>
        /// <param name="offset">The new position within the stream. This is relative to the <paramref name="origin"/> parameter, and can be positive or negative.</param>
        /// <param name="origin">A value of type <see cref="SeekOrigin"/>, which acts as the seek reference point.</param>
        /// <exception cref="IOException">Seeking was attempted before the beginning of the stream.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0L)
                        throw new IOException("Seek was attempted before the beginning of the stream.");

                    m_position = offset;
                    break;
                case SeekOrigin.Current:
                    if (m_position + offset < 0L)
                        throw new IOException("Seek was attempted before the beginning of the stream.");

                    m_position += offset;
                    break;
                case SeekOrigin.End:
                    if (m_length + offset < 0L)
                        throw new IOException("Seek was attempted before the beginning of the stream.");

                    m_position = m_length + offset;
                    break;
            }

            // Note: the length is not adjusted after this seek to reflect what MemoryStream.Seek does
            return m_position;
        }

        /// <summary>
        /// Sets the length of the current stream to the specified value.
        /// </summary>
        /// <param name="value">The value at which to set the length.</param>
        /// <remarks>
        /// If this length is larger than the previous length, the data is initialized to 0's between the previous length and the current length.
        /// </remarks>
        public override void SetLength(long value)
        {
            if (value > m_capacity)
                EnsureCapacity(value);

            if (m_length < value)
                InitializeToPosition(value);

            m_length = value;

            if (m_position > m_length)
                m_position = m_length;
        }

        /// <summary>
        /// Reads a block of bytes from the current stream and writes the data to <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified byte array with the values between <paramref name="startIndex"/> and (<paramref name="startIndex"/> + <paramref name="length"/> - 1) replaced by the characters read from the current stream.</param>
        /// <param name="startIndex">The byte offset in <paramref name="buffer"/> at which to begin reading.</param>
        /// <param name="length">The maximum number of bytes to read.</param>
        /// <returns>
        /// The total number of bytes written into the buffer. This can be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached before any bytes are read.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> or <paramref name="length"/> is less than 0 -or- 
        /// <paramref name="startIndex"/> and <paramref name="length"/> will exceed <paramref name="buffer"/> length.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override int Read(byte[] buffer, int startIndex, int length)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

            buffer.ValidateParameters(startIndex, length);

            // Do not read beyond the end of the stream
            long remainingBytes = m_length - m_position;

            if (remainingBytes <= 0)
                return 0;

            if (length > remainingBytes)
                length = (int)remainingBytes;

            int bytesRead = length;

            // Must read 1 block at a time
            do
            {
                int blockOffset = (int)(m_position & BlockMask);
                int bytesToRead = Math.Min(length, BlockSize - blockOffset);

                Buffer.BlockCopy(m_blocks[(int)(m_position >> ShiftBits)], blockOffset, buffer, startIndex, bytesToRead);

                length -= bytesToRead;
                startIndex += bytesToRead;
                m_position += bytesToRead;
            }
            while (length > 0);

            return bytesRead;
        }

        /// <summary>
        /// Reads a byte from the current stream.
        /// </summary>
        /// <returns>
        /// The current byte cast to an <see cref="int"/>, or -1 if the end of the stream has been reached.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override int ReadByte()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

            if (m_position >= m_length)
                return -1;

            byte value = m_blocks[(int)(m_position >> ShiftBits)][(int)(m_position & BlockMask)];
            m_position++;

            return value;
        }

        /// <summary>
        /// Writes a block of bytes to the current stream using data read from <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="startIndex">The byte offset in <paramref name="buffer"/> at which to begin writing from.</param>
        /// <param name="length">The maximum number of bytes to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> or <paramref name="length"/> is less than 0 -or- 
        /// <paramref name="startIndex"/> and <paramref name="length"/> will exceed <paramref name="buffer"/> length.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override void Write(byte[] buffer, int startIndex, int length)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

            buffer.ValidateParameters(startIndex, length);

            if (m_position + length > m_capacity)
                EnsureCapacity(m_position + length);

            if (m_position > m_length)
                InitializeToPosition(m_position);

            if (m_length < m_position + length)
                m_length = m_position + length;

            if (length == 0)
                return;

            do
            {
                int blockOffset = (int)(m_position & BlockMask);
                int bytesToWrite = Math.Min(length, BlockSize - blockOffset);

                Buffer.BlockCopy(buffer, startIndex, m_blocks[(int)(m_position >> ShiftBits)], blockOffset, bytesToWrite);

                length -= bytesToWrite;
                startIndex += bytesToWrite;
                m_position += bytesToWrite;
            }
            while (length > 0);
        }

        /// <summary>
        /// Writes a byte to the current stream at the current position.
        /// </summary>
        /// <param name="value">The byte to write.</param>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override void WriteByte(byte value)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

            if (m_position + 1 > m_capacity)
                EnsureCapacity(m_position + 1);

            if (m_position > m_length)
                InitializeToPosition(m_position);

            if (m_length < m_position + 1)
                m_length = m_position + 1;

            m_blocks[(int)(m_position >> ShiftBits)][m_position & BlockMask] = value;
            m_position++;
        }

        /// <summary>
        /// Writes the stream contents to a byte array, regardless of the <see cref="Position"/> property.
        /// </summary>
        /// <returns>A <see cref="byte"/>[] containing the current data in the stream</returns>
        /// <remarks>
        /// This may fail if there is not enough contiguous memory available to hold current size of stream.
        /// When possible use methods which operate on streams directly instead.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Cannot create a byte array with more than 2,147,483,591 elements.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public byte[] ToArray()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

            if (m_length > 0x7FFFFFC7L)
                throw new InvalidOperationException($"Cannot create a byte array of size {m_length}");

            byte[] destination = new byte[m_length];
            long originalPosition = m_position;

            m_position = 0;
            Read(destination, 0, (int)m_length);
            m_position = originalPosition;

            return destination;
        }

        /// <summary>
        /// Reads specified number of bytes from source stream into this <see cref="BlockAllocatedMemoryStream"/>
        /// starting at the current position.
        /// </summary>
        /// <param name="source">The stream containing the data to copy</param>
        /// <param name="length">The number of bytes to copy</param>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public void ReadFrom(Stream source, long length)
        {
            // Note: A faster way would be to write directly to the BlockAllocatedMemoryStream
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

            byte[] buffer = s_memoryBlockPool.Dequeue();

            do
            {
                int bytesRead = source.Read(buffer, 0, (int)Math.Min(BlockSize, length));

                if (bytesRead == 0)
                    throw new EndOfStreamException();

                length -= bytesRead;
                Write(buffer, 0, bytesRead);
            }
            while (length > 0);

            s_memoryBlockPool.Enqueue(buffer);
        }

        /// <summary>
        /// Writes the entire stream into destination, regardless of <see cref="Position"/>, which remains unchanged.
        /// </summary>
        /// <param name="destination">The stream onto which to write the current contents.</param>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public void WriteTo(Stream destination)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BlockAllocatedMemoryStream), "The stream is closed.");

            long originalPosition = m_position;
            m_position = 0;

            CopyTo(destination);

            m_position = originalPosition;
        }

        /// <summary>
        /// Overrides the <see cref="Stream.Flush"/> method so that no action is performed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method overrides the <see cref="Stream.Flush"/> method.
        /// </para>
        /// <para>
        /// Because any data written to a <see cref="BlockAllocatedMemoryStream"/> object is
        /// written into RAM, this method is superfluous.
        /// </para>
        /// </remarks>
        public override void Flush()
        {
            // Nothing to flush...
        }

        /// <summary>
        /// Makes sure desired <paramref name="length"/> can be accommodated by future data accesses.
        /// </summary>
        /// <param name="length">Minimum desired stream capacity.</param>
        private void EnsureCapacity(long length)
        {
            while (m_capacity < length)
            {
                m_blocks.Add(s_memoryBlockPool.Dequeue());
                m_capacity += BlockSize;
            }
        }

        /// <summary>
        /// Initializes all of the bytes to zero.
        /// </summary>
        private void InitializeToPosition(long position)
        {
            long bytesToClear = position - m_length;

            while (bytesToClear > 0)
            {
                int bytesToClearInBlock = (int)Math.Min(bytesToClear, BlockSize - (m_length & BlockMask));
                Array.Clear(m_blocks[(int)(m_length >> ShiftBits)], (int)(m_length & BlockMask), bytesToClearInBlock);
                m_length += bytesToClearInBlock;
                bytesToClear = position - m_length;
            }
        }

        #endregion

        #region [ Static ]

        // Static Fields

        // Allow up to 100 items of 8KB items to remain on the buffer pool. This might need to be increased if the buffer pool becomes more 
        // extensively used. Allocation Statistics will be logged in the Logger.
        private static readonly BufferPool s_memoryBlockPool = new(BlockSize, 100);

        #endregion
    }

    #endregion

    #region [ Buffer Pool ]

    /// <summary>
    /// Provides a thread safe queue that acts as a buffer pool. 
    /// </summary>
    internal class BufferPool
    {
        private readonly int m_bufferSize;
        private readonly ConcurrentQueue<byte[]> m_buffers;
        private readonly Queue<int> m_countHistory;
        private readonly int m_targetCount;
        private int m_objectsCreated;

        /// <summary>
        /// Creates a new <see cref="BufferPool"/>.
        /// </summary>
        /// <param name="bufferSize">The size of buffers in the pool.</param>
        /// <param name="targetCount">the ideal number of buffers that are always pending on the queue.</param>
        public BufferPool(int bufferSize, int targetCount)
        {
            m_bufferSize = bufferSize;
            m_targetCount = targetCount;
            m_countHistory = new Queue<int>(100);
            m_buffers = new ConcurrentQueue<byte[]>();

            new Action(RunCollection).DelayAndExecute(1000);
        }

        private void RunCollection()
        {
            try
            {
                m_countHistory.Enqueue(m_buffers.Count);

                if (m_countHistory.Count < 60)
                    return;

                int objectsCreated = Interlocked.Exchange(ref m_objectsCreated, 0);

                // If there were ever more than the target items in the queue over the past 60 seconds remove some items.
                // However, don't remove items if the pool ever got to 0 and had objects that had to be created.
                int min = m_countHistory.Min();
                m_countHistory.Clear();

                if (objectsCreated != 0)
                    return;

                while (min > m_targetCount)
                {
                    if (!m_buffers.TryDequeue(out _))
                        return;

                    min--;
                }
            }
            finally
            {
                new Action(RunCollection).DelayAndExecute(1000);
            }
        }

        /// <summary>
        /// Removes a buffer from the queue. If one does not exist, one is created.
        /// </summary>
        /// <returns></returns>
        public byte[] Dequeue()
        {
            if (m_buffers.TryDequeue(out byte[]? item))
                return item;

            Interlocked.Increment(ref m_objectsCreated);
            return new byte[m_bufferSize];
        }

        /// <summary>
        /// Adds a buffer back to the queue.
        /// </summary>
        /// <param name="buffer">The buffer to queue.</param>
        public void Enqueue(byte[] buffer) => m_buffers.Enqueue(buffer);
    }

    #endregion

}
