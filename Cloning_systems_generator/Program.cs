using System;
using System.Text;
using System.IO;

namespace Cloning_systems_generator
{
    class Program
    {
        private static string inputDirectory = @"C:\Users\dmitr\Source\Repos\Diophantine_sharp\Diophantine_sharp_0.1\";

        static void Main(string[] args)
        {
            int max_x_multiplier = 100;
            string inputFile = "input25x41.txt";

            string[] original = File.ReadAllLines(inputDirectory + inputFile);

            for (int y = original.Length; y > 0; y--)
            {
                for (int x_multiplier = 1; x_multiplier <= max_x_multiplier; x_multiplier++)
                {
                    string inputName = "input" + y.ToString() + "x" + original[0].Length.ToString() + ".txt";

                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(new System.IO.FileStream(inputName, FileMode.Create)))
                    {
                        for (int j = 0; j <= y; j++)
                        {
                            for (int i = 0; i < x_multiplier - 1; i++)
                            {
                                file.Write(original[j] + " ");
                            }

                            file.WriteLine(original[j]);
                        }

                    }

                }
            }
        }
    }
}
