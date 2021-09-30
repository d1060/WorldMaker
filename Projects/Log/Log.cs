using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Log
{
    static readonly string logFile = "E:\\Projects\\WorldMaker\\Logs\\WorldMaker.log";

    public static void Write(string logLine)
    {
#if DEBUG
        bool written = false;
        int attempt = 0;
        while (!written)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(logFile))
                {
                    sw.WriteLine(logLine);
                    written = true;
                }
            }
            catch (Exception)
            {

            }
            if (!written)
            {
                if (attempt >= 10)
                    return;

                Thread.Sleep(50);
                attempt++;
            }
        }
#endif
    }

    public static void Reset()
    {
#if DEBUG
        if (File.Exists(logFile))
            File.Delete(logFile);
#endif
    }
}

