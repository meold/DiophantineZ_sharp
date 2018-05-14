using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {

            string sourceDirectory = @"C:\Users\dmitr\source\repos\DiophantineZ_sharp\Generator\SourceSystems\";
            string sourceFile = "input40x61(10x30).txt";
            string inputDirectory = @"C:\Users\dmitr\source\repos\DiophantineZ_sharp\Generator\Input\"; //Directory for generated systems

            string[] original = File.ReadAllLines(sourceDirectory + sourceFile);


            int min_variables_multiplier = 1;
            int max_variables_multiplier = 100;

            int min_equations = 1;
            int max_equations = original.Length;

            for (int equations = max_equations; equations >= min_equations; equations--)
            {
                for (int variables_multiplier = min_variables_multiplier;
                    variables_multiplier <= max_variables_multiplier;
                    variables_multiplier++)
                {
                    string inputName = "input" + equations.ToString() + "x" +
                                       (original[0].Split().Length * variables_multiplier).ToString() + ".txt";

                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(
                            new System.IO.FileStream(inputDirectory + inputName, FileMode.Create)))
                    {
                        for (int j = 0; j < equations; j++)
                        {
                            for (int i = 0; i < variables_multiplier - 1; i++)
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
