/*
 * Modified by Davin Ockerby (Platinum Lucario) for use with GTK4
 * and VG Music Studio. Originally made by Fabrice Lacharme for use
 * on WinForms. Modified since 2023-08-04 at 00:32.
 */

#region Original License

/* Copyright (c) 2017 Fabrice Lacharme
 * This code is inspired from Michal Brylka 
 * https://www.codeproject.com/Articles/17395/Owner-drawn-trackbar-slider
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


using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kermalis.VGMusicStudio.GTK4.Util;

internal class ScaleControl : Adjustment
{
    internal Adjustment Instance { get; }

    internal ScaleControl(double value, double lower, double upper, double stepIncrement, double pageIncrement, double pageSize)
    {
        Instance = New(value, lower, upper, stepIncrement, pageIncrement, pageSize);
    }
    
    private double _smallChange = 1L;
    public double SmallChange
    {
        get => _smallChange;
        set
        {
            if (value >= 0)
            {
                _smallChange = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(SmallChange), $"{nameof(SmallChange)} must be greater than or equal to 0.");
            }
        }
    }
    private double _largeChange = 5L;
    public double LargeChange
    {
        get => _largeChange;
        set
        {
            if (value >= 0)
            {
                _largeChange = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(LargeChange), $"{nameof(LargeChange)} must be greater than or equal to 0.");
            }
        }
    }

}
