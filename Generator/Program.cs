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
        static string inputDirectory = @"C:\Users\dmitr\Documents\DiophantineZ\Input\";
        static string Add_to_name_if_exists(string filename) //add number in brackets (1) or (2) ... if file already exists
        {
            if (File.Exists(inputDirectory + filename))
            {
                int t = 1;
                while (File.Exists(inputDirectory + filename.Split('.').First() + "(" + t.ToString() + ")" + ".txt")) //add (1) if file exists, increase to (2) if filename(1).txt also exists
                {
                    t++;                   
                }
                filename = filename.Split('.').First() + "(" + t.ToString() + ")" + ".txt";
            }
            return filename;
        }
        static void Main(string[] args)
        {
            int max_variables = 150;
            //for (int variables = 4; variables < max_variables; variables++)
           // {
            int variables = 300;
                for (int equations = variables - 1; equations > variables / 6; equations-=5)
                {
                    string inputName = "input" + equations.ToString() + "x" + variables.ToString() + ".txt";
                    inputName = Add_to_name_if_exists(inputName);
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(new System.IO.FileStream(inputDirectory+inputName, FileMode.Create)))
                    {
                        Random rnd = new Random();
                        for (int i = 0; i < equations; i++)
                        {
                            int equation_sum = 0;
                            for (int j = 0; j < variables; j++)
                            {
                                file.Write(rnd.Next(-10, 10) + " ");
                            }
                            file.WriteLine();
                        }

                    }
                }
            //}
        }
    }
}
