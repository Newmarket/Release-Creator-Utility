using System;
using System.IO;

namespace EnvironmentConfigurationUpdater
{
    public static class Log
    {
        public static string fileName { get; set; }
        public static void ToFile(string output)
        {
            Console.WriteLine(output);
            using (StreamWriter sw = File.AppendText(@fileName))
            {
                sw.WriteLine(output);
                sw.Close();
            }

        }
    }
}
