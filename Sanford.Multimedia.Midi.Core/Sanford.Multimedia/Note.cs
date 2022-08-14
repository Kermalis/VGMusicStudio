#region License

/* Copyright (c) 2006 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

#region Contact

/*
 * Leslie Sanford
 * Email: jabberdabber@hotmail.com
 */

#endregion

namespace Sanford.Multimedia
{
    /// <summary>
    /// Defines constants representing the 12 Note of the chromatic scale.
    /// </summary>
    public enum Note
    {
        /// <summary>
        /// C natural.
        /// </summary>
        C, 

        /// <summary>
        /// C sharp.
        /// </summary>
        CSharp,

        /// <summary>
        /// D flat.
        /// </summary>
        DFlat = CSharp,

        /// <summary>
        /// D natural.
        /// </summary>
        D,

        /// <summary>
        /// D sharp.
        /// </summary>
        DSharp,

        /// <summary>
        /// E flat.
        /// </summary>
        EFlat = DSharp,

        /// <summary>
        /// E natural.
        /// </summary>
        E,

        /// <summary>
        /// F natural.
        /// </summary>
        F,

        /// <summary>
        /// F sharp.
        /// </summary>
        FSharp,

        /// <summary>
        /// G flat.
        /// </summary>
        GFlat = FSharp,

        /// <summary>
        /// G natural.
        /// </summary>
        G,

        /// <summary>
        /// G sharp.
        /// </summary>
        GSharp,

        /// <summary>
        /// A flat.
        /// </summary>
        AFlat = GSharp,

        /// <summary>
        /// A natural.
        /// </summary>
        A,

        /// <summary>
        /// A sharp.
        /// </summary>
        ASharp,

        /// <summary>
        /// B flat.
        /// </summary>
        BFlat = ASharp,

        /// <summary>
        /// B natural.
        /// </summary>
        B
    }
}
