using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Drawing;
using System.Drawing.Imaging;

public partial class ImageTools
{
    public static void SaveTextureRawCubemapFloatArray(float[] floatArray, int width, string fileName, float divisor = 1)
    {
        try
        {
            short[] shortArray = new short[width * width * 6 * 3];

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < 6 * width; x++)
                {
                    int cubemapIndex = x + y * 6 * width;
                    int colorIndex = (x + (width - y - 1) * 6 * width) * 3;
                    float value = floatArray[cubemapIndex] / divisor;

                    if (value < 0) value = 0;
                    if (value > 1) value = 1;

                    SetByteArrayColor(ref shortArray, colorIndex, value);
                }
            }

            Bitmap b = new Bitmap(6 * width, width, PixelFormat.Format48bppRgb);
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, 6 * width, width),
                    ImageLockMode.ReadWrite,
                    b.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int length = bmData.Stride * b.Height / 2;

            // Copy the RGB values back to the bitmap
            Marshal.Copy(shortArray, 0, ptr, length);

            b.UnlockBits(bmData);

            b.Save(fileName, ImageFormat.Png);
        }
        catch (Exception e)
        {
            Log.Write(DateTime.Now.ToString() + " Error saving image " + fileName + ".\n" + e.Message + ".\n" + e.StackTrace);
        }
    }

    public static void SaveTextureCubemapFloatArray(float[] floatArray, int width, string fileName)
    {
        try
        {
            short[] shortArray = new short[width * width * 2 * 3];

            for (int y = 0; y < width; y++)
            {
                float v = (float)y / width;
                for (int x = 0; x < 2 * width; x++)
                {
                    float u = (float)(2 * width - x - 1) / (2 * width);
                    Vector2 uv = new Vector2(u, v);
                    Vector3 cartesian = uv.PolarRatioToCartesian(1);
                    Vector3 cubemap = cartesian.CartesianToCubemap();
                    cubemap.x *= width;
                    cubemap.y *= width;

                    int cubemapIndex = (int)(Mathf.Floor(cubemap.x) + cubemap.z * width + Mathf.Floor(cubemap.y) * 6 * width);
                    int colorIndex = (x + (width - y - 1) * 2 * width) * 3;
                    float value = 0;
                    if (cubemapIndex >= 0 && cubemapIndex < floatArray.Length)
                        value = floatArray[cubemapIndex];

                    if (value < 0) value = 0;
                    if (value > 1) value = 1;

                    SetByteArrayColor(ref shortArray, colorIndex, value);
                }
            }

            Bitmap b = new Bitmap(2 * width, width, PixelFormat.Format48bppRgb);
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, 2 * width, width),
                    ImageLockMode.ReadWrite,
                    b.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int length = bmData.Stride * b.Height / 2;

            // Copy the RGB values back to the bitmap
            Marshal.Copy(shortArray, 0, ptr, length);

            b.UnlockBits(bmData);

            b.Save(fileName, ImageFormat.Png);
        }
        catch (Exception e)
        {
            Log.Write(DateTime.Now.ToString() + " Error saving image " + fileName + ".\n" + e.Message + ".\n" + e.StackTrace);
        }
    }

    public static void SaveTextureCubemapFaceFloatArray(float[] floatArray, int width, string fileName, float divisor = 1)
    {
        try
        {
            short[] shortArray = new short[width * width * 3];

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (x + (width - y - 1) * width);
                    int colorIndex = index * 3;
                    float value = 0;
                    if (index >= 0 && index < floatArray.Length)
                        value = floatArray[index] / divisor;

                    if (value < 0) value = 0;
                    if (value > 1) value = 1;

                    SetByteArrayColor(ref shortArray, colorIndex, value);
                }
            }

            Bitmap b = new Bitmap(width, width, PixelFormat.Format48bppRgb);
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, width, width),
                    ImageLockMode.ReadWrite,
                    b.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int length = bmData.Stride * b.Height / 2;

            // Copy the RGB values back to the bitmap
            Marshal.Copy(shortArray, 0, ptr, length);

            b.UnlockBits(bmData);

            b.Save(fileName, ImageFormat.Png);
        }
        catch (Exception e)
        {
            Log.Write(DateTime.Now.ToString() + " Error saving image " + fileName + ".\n" + e.Message + ".\n" + e.StackTrace);
        }
    }

    public static void SaveTextureFloatArray(float[] floatArray, int width, string fileName, float divisor = 1)
    {
        try
        {
            int height = floatArray.Length / width;
            short[] shortArray = new short[width * height * 3];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (x + (height - y - 1) * width);
                    int mirroredIndex = (x + y * width);
                    int colorIndex = mirroredIndex * 3;
                    float value = 0;
                    if (index >= 0 && index < floatArray.Length)
                        value = floatArray[index] / divisor;

                    if (value < 0) value = 0;
                    if (value > 1) value = 1;

                    SetByteArrayColor(ref shortArray, colorIndex, value);
                }
            }

            Bitmap b = new Bitmap(width, height, PixelFormat.Format48bppRgb);
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite,
                    b.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int length = bmData.Stride * b.Height / 2;

            // Copy the RGB values back to the bitmap
            Marshal.Copy(shortArray, 0, ptr, length);

            b.UnlockBits(bmData);

            b.Save(fileName, ImageFormat.Png);
        }
        catch (Exception e)
        {
            Log.Write(DateTime.Now.ToString() + " Error saving image " + fileName + ".\n" + e.Message + ".\n" + e.StackTrace);
        }
    }

    public static void SaveTextureCubemapFaceUIntArray(uint[] uintArray, int width, string fileName, float divisor = 1)
    {
        try
        {
            short[] shortArray = new short[width * width * 3];

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (x + (width - y - 1) * width);
                    int colorIndex = index * 3;
                    float value = 0;
                    if (index >= 0 && index < uintArray.Length)
                        value = uintArray[index] / divisor;

                    if (value < 0) value = 0;
                    if (value > 1) value = 1;

                    SetByteArrayColor(ref shortArray, colorIndex, value);
                }
            }

            Bitmap b = new Bitmap(width, width, PixelFormat.Format48bppRgb);
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, width, width),
                    ImageLockMode.ReadWrite,
                    b.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int length = bmData.Stride * b.Height / 2;

            // Copy the RGB values back to the bitmap
            Marshal.Copy(shortArray, 0, ptr, length);

            b.UnlockBits(bmData);

            b.Save(fileName, ImageFormat.Png);
        }
        catch (Exception e)
        {
            Log.Write(DateTime.Now.ToString() + " Error saving image " + fileName + ".\n" + e.Message + ".\n" + e.StackTrace);
        }
    }

    public static void SaveTextureUIntArray(uint[] uintArray, int width, string fileName, float divisor = 1)
    {
        try
        {
            int height = uintArray.Length / width;
            short[] shortArray = new short[width * height * 3];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (x + (height - y - 1) * width);
                    int mirroredIndex = (x + y * width);
                    int colorIndex = mirroredIndex * 3;
                    float value = 0;
                    if (index >= 0 && index < uintArray.Length)
                        value = uintArray[index] / divisor;

                    if (value < 0) value = 0;
                    if (value > 1) value = 1;

                    SetByteArrayColor(ref shortArray, colorIndex, value);
                }
            }

            Bitmap b = new Bitmap(width, height, PixelFormat.Format48bppRgb);
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite,
                    b.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int length = bmData.Stride * b.Height / 2;

            // Copy the RGB values back to the bitmap
            Marshal.Copy(shortArray, 0, ptr, length);

            b.UnlockBits(bmData);

            b.Save(fileName, ImageFormat.Png);
        }
        catch (Exception e)
        {
            Log.Write(DateTime.Now.ToString() + " Error saving image " + fileName + ".\n" + e.Message + ".\n" + e.StackTrace);
        }
    }

    public static void SaveTextureUIntArray(uint[] uintArray, int width, int height, string fileName, float divisor = 1)
    {
        try
        {
            short[] shortArray = new short[width * width * 3];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (x + (height - y - 1) * width);
                    int colorIndex = index * 3;
                    float value = 0;
                    if (index >= 0 && index < uintArray.Length)
                        value = uintArray[index] / divisor;

                    if (value < 0) value = 0;
                    if (value > 1) value = 1;

                    SetByteArrayColor(ref shortArray, colorIndex, value);
                }
            }

            Bitmap b = new Bitmap(width, height, PixelFormat.Format48bppRgb);
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite,
                    b.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int length = bmData.Stride * b.Height / 2;

            // Copy the RGB values back to the bitmap
            Marshal.Copy(shortArray, 0, ptr, length);

            b.UnlockBits(bmData);

            b.Save(fileName, ImageFormat.Png);
        }
        catch (Exception e)
        {
            Log.Write(DateTime.Now.ToString() + " Error saving image " + fileName + ".\n" + e.Message + ".\n" + e.StackTrace);
        }
    }

    static void SetByteArrayColor(ref short[] byteArray, int index, float floatVal)
    {
        short byteValue = (short)(floatVal * UInt16.MaxValue);
        byteArray[index] = byteValue;
        byteArray[index + 1] = byteValue;
        byteArray[index + 2] = byteValue;
    }
}
