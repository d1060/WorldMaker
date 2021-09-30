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
    public static void SaveTextureFloatArray(float[] floatArray, int width, int height, string fileName)
    {
        try
        {
            short[] shortArray = new short[width * height * 3];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    int colorIndex = (x + (height - y - 1) * width) * 3;
                    float value = floatArray[index];
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
