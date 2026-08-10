using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal abstract class MP2KResampler
{
    protected List<float> FetchBuffer = [];
    protected float Phase = 0.0f;

    protected const ushort INTERP_FILTER_SIZE = 16;
    protected const float INTERP_FILTER_CUTOFF_FREQ = 0.85f;
    protected const ushort INTERP_FILTER_LUT_SIZE = 256;
    protected const ushort INTEGRAL_RESOLUTION = 256;

    internal abstract bool Process(Span<float> buffer, float phaseInc, FetchCallback fetchCallback);
    internal abstract void Reset();

    internal delegate bool FetchCallback(ref List<float> fetchBuffer, int samplesRequired);

    internal static MP2KResampler MakeResampler(ResamplerType t)
    {
        return t switch
        {
            ResamplerType.Nearest => new NearestResampler(),
            ResamplerType.Linear => new LinearResampler(),
            // ResamplerType.Sinc => new SincResampler(),
            // ResamplerType.Blep => new BlepResampler(),
            // ResamplerType.Blamp => new BlampResampler(),
            _ => throw new Exception("Invalid Resampler Type"),
        };
    }
}

internal class NearestResampler : MP2KResampler
{
    internal NearestResampler()
    {
    }

    internal override void Reset()
    {
        FetchBuffer.Clear();
        Phase = 0.0f;
    }

    internal override bool Process(Span<float> buffer, float phaseInc, FetchCallback fetchCallback)
    {
        if (buffer.Length == 0)
            return true;

        phaseInc = Math.Max(phaseInc, 0.0f);

        int samplesRequired = (int)(Phase + phaseInc * buffer.Length);
        // be sure and fetch one more sample in case of odd rounding errors
        samplesRequired += 1;
        bool continuePlayback = fetchCallback(ref FetchBuffer, samplesRequired);

        int fi = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (i >= FetchBuffer.Count)
            {
                break;
            }
            buffer[i] = FetchBuffer[fi];
            Phase += phaseInc;
            int istep = (int)Phase;
            Phase -= istep;
            fi += istep;
        }

        // remove first fi elements from the fetch buffer since they are no longer needed
        FetchBuffer.RemoveRange(0, fi);

        return continuePlayback;
    }
}

internal class LinearResampler : MP2KResampler
{
    internal LinearResampler()
    {
        Reset();
    }

    internal override void Reset()
    {
        FetchBuffer.Clear();
        Phase = 0.0f;
    }

    internal override bool Process(Span<float> buffer, float phaseInc, FetchCallback fetchCallback)
    {
        if (buffer.Length == 0)
            return true;

        phaseInc = Math.Max(phaseInc, 0.0f);

        int samplesRequired = (int)(Phase + phaseInc * buffer.Length);
        // be sure and fetch one more sample in case of odd rounding errors
        samplesRequired += 1;
        // fetch one more for linear interpolation
        samplesRequired += 1;
        bool continuePlayback = fetchCallback(ref FetchBuffer, samplesRequired);

        int fi = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            float a = FetchBuffer[fi];
            float b = FetchBuffer[fi + 1];
            buffer[i] = a + Phase * (b - a);
            Phase += phaseInc;
            int istep = (int)Phase;
            Phase -= istep;
            fi += istep;
        }

        // remove first fi elements from the fetch buffer since they are no longer needed
        FetchBuffer.RemoveRange(0, fi);

        return continuePlayback;
    }
}

// internal class SincResampler : MP2KResampler
// {
//     static float[] cosLut
//     {
//         get
//         {
//             float[] l = new float[INTERP_FILTER_LUT_SIZE];
//             for (int i = 0; i < l.Length; i++)
//             {
//                 float index = i * (float)(2.0 * Math.PI / INTERP_FILTER_LUT_SIZE);
//                 l[i] = MathF.Cos(index);
//             }
//             return l;
//         }
//     }
//     static float[] sincLut
//     {
//         get
//         {
//             float[] l = new float[INTERP_FILTER_LUT_SIZE + 2];
//             for (int i = 0; i < INTERP_FILTER_LUT_SIZE + 1; i++)
//             {
//                 float index = i * (float)(INTERP_FILTER_SIZE * Math.PI / INTERP_FILTER_LUT_SIZE);
//                 l[i] = boost::math::sinc_pi(index);
//             }
//             l[INTERP_FILTER_LUT_SIZE + 1] = 0.0f;
//             return l;
//         }
//     }
//     static float[] winLut
//     {
//         get
//         {
//             // hann window (raised cosine)
//             float[] l = new float[INTERP_FILTER_LUT_SIZE + 2];
//             for (int i = 0; i < INTERP_FILTER_LUT_SIZE + 1; i++)
//             {
//                 float index = i * (float)(Math.PI / INTERP_FILTER_LUT_SIZE);
//                 l[i] = 0.5f + (0.5f * MathF.Cos(index));
//             }
//             l[INTERP_FILTER_LUT_SIZE + 1] = 0.0f;
//             return l;
//         }
//     }

//     internal SincResampler()
//     {
//         Reset();
//     }

//     internal override void Reset()
//     {
//         fetchBuffer.Clear();
//         phase = 0.0f;
//     }

//     bool Process(Span<float> buffer, float phaseInc, FetchCallback fetchCallback)
//     {
//         if (buffer.Length == 0)
//             return true;

//         phaseInc = Math.Max(phaseInc, 0.0f);

//         int samplesRequired = (int)(phase + phaseInc * buffer.Length);
//         // be sure and fetch one more sample in case of odd rounding errors
//         samplesRequired += 1;
//         // fetch a few more for complete windowed sinc interpolation
//         samplesRequired += INTERP_FILTER_SIZE * 2;
//         bool continuePlayback = fetchCallback(ref fetchBuffer, samplesRequired);
//         float sincStep = phaseInc > INTERP_FILTER_CUTOFF_FREQ ? INTERP_FILTER_CUTOFF_FREQ / phaseInc : 1.00f;

//         int fi = 0;
//         for (int i = 0; i < buffer.Length; i++)
//         {
//             float sampleSum = 0.0f;
//             float kernelSum = 0.0f;

//             for (int wi = -INTERP_FILTER_SIZE + 1; wi <= INTERP_FILTER_SIZE; wi++)
//             {
//                 float sincIndex = (wi - phase) * sincStep;
//                 float windowIndex = wi - phase;
//                 float s = fast_sincf(sincIndex);
//                 float w = window_func(windowIndex);
//                 float kernel = s * w;
//                 sampleSum += kernel * fetchBuffer[fi + wi + INTERP_FILTER_SIZE - 1];
//                 kernelSum += kernel;
//             }

//             phase += phaseInc;
//             int istep = (int)phase;
//             phase -= istep;
//             fi += istep;

//             buffer[i] = sampleSum / kernelSum;
//         }

//         // remove first fi elements from the fetch buffer since they are no longer needed
//         fetchBuffer.RemoveRange(0, fi);

//         return continuePlayback;
//     }

//     private float fast_sinf(float t)
//     {
//         return fast_cosf(t - (float)(Math.PI / 2.0));
//     }

//     private float fast_cosf(float t)
//     {
//         t = Math.Abs(t);
//         t *= (float)(INTERP_FILTER_LUT_SIZE / (2.0 * Math.PI));
//         uint left_index = (uint)t;
//         float fraction = t - left_index;
//         uint right_index = (left_index + 1) % INTERP_FILTER_LUT_SIZE;
//         left_index %= INTERP_FILTER_LUT_SIZE;
//         return cosLut[left_index] + fraction * (cosLut[right_index] - cosLut[left_index]);
//     }

//     private float fast_sincf(float t)
//     {
//         t = Math.Abs(t);
//         // assert(t <= INTERP_FILTER_SIZE);
//         t *= (float)(INTERP_FILTER_LUT_SIZE / (double)INTERP_FILTER_SIZE);
//         uint left_index = (uint)t;
//         float fraction = t - left_index;
//         uint right_index = left_index + 1;
//         return sincLut[left_index] + fraction * (sincLut[right_index] - sincLut[left_index]);
//     }

//     private float window_func(float t)
//     {
//         // assert(t >= -float(INTERP_FILTER_SIZE));
//         // assert(t <= +float(INTERP_FILTER_SIZE));
//         t = Math.Abs(t);
//         t *= (float)(INTERP_FILTER_LUT_SIZE / (double)INTERP_FILTER_SIZE);
//         uint left_index = (uint)t;
//         float fraction = t - left_index;
//         uint right_index = left_index + 1;
//         return winLut[left_index] + fraction * (winLut[right_index] - winLut[left_index]);
//     }

// }

// internal class BlepResampler : MP2KResampler
// {
//     static float[] SiLut
//     {
//         get
//         {
//             float[] l = new float[INTERP_FILTER_LUT_SIZE + 2];
//             double acc = 0.0;
//             double step_per_index = INTERP_FILTER_SIZE / (double)INTERP_FILTER_LUT_SIZE;
//             double integration_inc = step_per_index / INTEGRAL_RESOLUTION;
//             double index = 0.0;
//             double prev_value = 1.0;

//             for (int i = 0; i < INTERP_FILTER_LUT_SIZE + 1; i++)
//             {
//                 double convergence_level = 0.5 - 0.5 * Math.Cos(i * Math.PI / INTERP_FILTER_LUT_SIZE);
//                 double integral_level = 1.0 - convergence_level;
//                 l[i] = (float)(acc * integral_level + 0.5 * convergence_level);
//                 for (int j = 0; j < INTEGRAL_RESOLUTION; j++)
//                 {
//                     index += integration_inc;
//                     double new_value = boost::math::sinc_pi(Math.PI * index);
//                     acc += (new_value + prev_value) * integration_inc * 0.5;
//                     prev_value = new_value;
//                 }
//             }
//             l[INTERP_FILTER_LUT_SIZE + 1] = 0.5f;
//             return l;
//         }
//     }

//     internal BlepResampler()
//     {
//         Reset();
//     }

//     internal override void Reset()
//     {
//         fetchBuffer.Clear();

//         phase = 0.0f;
//     }

//     bool Process(Span<float> buffer, float phaseInc, FetchCallback fetchCallback)
//     {
//         if (buffer.Length == 0)
//             return true;

//         phaseInc = Math.Max(phaseInc, 0.0f);

//         int samplesRequired = (int)(phase + phaseInc * buffer.Length);
//         // be sure and fetch one more sample in case of odd rounding errors
//         samplesRequired += 1;
//         // fetch a few more for complete windowed sinc interpolation
//         samplesRequired += INTERP_FILTER_SIZE * 2;
//         bool continuePlayback = fetchCallback(ref fetchBuffer, samplesRequired);
//         float sincStep = INTERP_FILTER_CUTOFF_FREQ / phaseInc;

//         int fi = 0;
//         for (int i = 0; i < buffer.Length; i++)
//         {
//             float sampleSum = 0.0f;
//             float kernelSum = 0.0f;

//             float sl = fast_Si((-INTERP_FILTER_SIZE + 1 - phase - 0.5f) * sincStep);

//             for (int wi = -INTERP_FILTER_SIZE + 1; wi <= INTERP_FILTER_SIZE; wi++)
//             {
//                 float sr = fast_Si((wi - phase + 0.5f) * sincStep);
//                 float kernel = sr - sl;
//                 sampleSum += kernel * fetchBuffer[fi + wi + INTERP_FILTER_SIZE - 1];
//                 kernelSum += kernel;
//                 sl = sr;
//             }

//             phase += phaseInc;
//             int istep = (int)phase;
//             phase -= istep;
//             fi += istep;

//             buffer[i] = sampleSum / kernelSum;
//         }

//         // remove first i elements from the fetch buffer since they are no longer needed
//         fetchBuffer.RemoveRange(0, fi);

//         return continuePlayback;
//     }

//     internal static float fast_Si(float t)
//     {
//         float signed_t = t;
//         t = Math.Abs(t);
//         t = Math.Min(t, INTERP_FILTER_SIZE);
//         t *= (float)(INTERP_FILTER_LUT_SIZE / (double)INTERP_FILTER_SIZE);
//         uint left_index = (uint)t;
//         float fraction = t - left_index;
//         uint right_index = left_index + 1;
//         float retval = SiLut[left_index] + fraction * (SiLut[right_index] - SiLut[left_index]);
//         return std::copysignf(retval, signed_t);
//     }
// }

// internal class BlampResampler : MP2KResampler
// {
//     static float[] TiLut
//     {
//         get
//         {
//             float[] l = new float[INTERP_FILTER_LUT_SIZE + 2];
//             double acc = 0.0;
//             double step_per_index = INTERP_FILTER_SIZE / (double)INTERP_FILTER_LUT_SIZE;
//             double integration_inc = step_per_index / INTEGRAL_RESOLUTION;
//             double index = 0.0;
//             double prev_value = 1.0;

//             for (int i = 0; i < INTERP_FILTER_LUT_SIZE + 1; i++)
//             {
//                 double t = i * step_per_index;
//                 double convergence_value = t * 0.5;
//                 double function_value = t * acc + Math.Cos(Math.PI * t) / (Math.PI * Math.PI);
//                 double interpolation_t = 0.5 - 0.5 * Math.Cos(i * Math.PI / INTERP_FILTER_LUT_SIZE);
//                 double interpolated_value = function_value + interpolation_t * (convergence_value - function_value);
//                 l[i] = (float)interpolated_value;

//                 for (int j = 0; j < INTEGRAL_RESOLUTION; j++)
//                 {
//                     index += integration_inc;
//                     double new_value = boost::math::sinc_pi(Math.PI * index);
//                     acc += (new_value + prev_value) * integration_inc * 0.5;
//                     prev_value = new_value;
//                 }
//             }
//             l[INTERP_FILTER_LUT_SIZE + 1] = (float)((INTERP_FILTER_LUT_SIZE + 1) * step_per_index * 0.5);
//             return l;
//         }
//     }
//     static float fast_Ti(float t)
//     {
//         t = Math.Abs(t);
//         float old_t = t;
//         t = Math.Min(t, INTERP_FILTER_SIZE);
//         t *= (float)(INTERP_FILTER_LUT_SIZE / (double)INTERP_FILTER_SIZE);
//         uint left_index = (uint)t;
//         float fraction = t - left_index;
//         uint right_index = left_index + 1;
//         float retval = TiLut[left_index] + fraction * (TiLut[right_index] - TiLut[left_index]);
//         if (old_t > INTERP_FILTER_SIZE)
//             return old_t * 0.5f;
//         else
//             return retval;
//     }

//     internal BlampResampler()
//     {
//         Reset();
//     }

//     internal override void Reset()
//     {
//         fetchBuffer.Clear();

//         phase = 0.0f;
//     }

//     bool Process(Span<float> buffer, float phaseInc, FetchCallback fetchCallback)
//     {
//         if (buffer.Length == 0)
//             return true;

//         phaseInc = Math.Max(phaseInc, 0.0f);

//         int samplesRequired = (int)(phase + phaseInc * buffer.Length);
//         // be sure and fetch one more sample in case of odd rounding errors
//         samplesRequired += 1;
//         // fetch a few more for complete windowed sinc interpolation
//         samplesRequired += INTERP_FILTER_SIZE * 2;
//         bool continuePlayback = fetchCallback(ref fetchBuffer, samplesRequired);
//         float sincStep = INTERP_FILTER_CUTOFF_FREQ / phaseInc;

//         int fi = 0;
//         for (int i = 0; i < buffer.Length; i++)
//         {
//             float sampleSum = 0.0f;
//             float kernelSum = 0.0f;

//             float sl = fast_Ti((-INTERP_FILTER_SIZE + 1 - phase - 1.0f) * sincStep);
//             float sm = fast_Ti((-INTERP_FILTER_SIZE + 1 - phase) * sincStep);

//             for (int wi = -INTERP_FILTER_SIZE + 1; wi <= INTERP_FILTER_SIZE; wi++)
//             {
//                 float TiIndexRight = (wi - phase + 1.0f) * sincStep;
//                 float sr = fast_Ti(TiIndexRight);
//                 float kernel = sr - 2.0f * sm + sl;
//                 sampleSum += kernel * fetchBuffer[fi + wi + INTERP_FILTER_SIZE - 1];
//                 kernelSum += kernel;
//                 sl = sm;
//                 sm = sr;
//             }

//             phase += phaseInc;
//             int istep = (int)phase;
//             phase -= istep;
//             fi += istep;

//             buffer[i] = sampleSum / kernelSum;
//         }

//         // remove first i elements from the fetch buffer since they are no longer needed
//         fetchBuffer.RemoveRange(0, fi);

//         return continuePlayback;
//     }
// }
