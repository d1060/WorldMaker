using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Utils
{
    public static float LowestOf(float[] fs, out int lowestIndex)
    {
        float lowestValue = float.MaxValue;
        lowestIndex = fs.Length;
        for (int i = 0; i < fs.Length; i++)
        {
            float f = fs[i];
            if (f < lowestValue)
            {
                lowestValue = f;
                lowestIndex = i + 1;
            }
        }

        return lowestValue;
    }

    public static float HighestOf(float[] fs, out int highestIndex)
    {
        float highestValue = float.MinValue;
        highestIndex = fs.Length;
        for (int i = 0; i < fs.Length; i++)
        {
            float f = fs[i];
            if (f > highestValue)
            {
                highestValue = f;
                highestIndex = i + 1;
            }
        }

        return highestValue;
    }

    public static double HighestOf(double[] fs, out int highestIndex)
    {
        double highestValue = double.MinValue;
        highestIndex = fs.Length;
        for (int i = 0; i < fs.Length; i++)
        {
            double f = fs[i];
            if (f > highestValue)
            {
                highestValue = f;
                highestIndex = i + 1;
            }
        }

        return highestValue;
    }
}
