using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DiophantineZ_sharp
{
    class Program
    {
        static void write_arr(int[] arr)  //printing array to console with commas
        {
            foreach (var a in arr)
            {
                Console.Write(a + ", ");
            }
            Console.WriteLine();
        }
        public static List<int[]> Read_file(string path)  //read input matrix
        {
            string[] str_input = File.ReadAllLines(path);
            List<int[]> retlist = new List<int[]>();
            foreach (string line in str_input)
            {
                    string[] arline = line.Split();
                    int[] intline = new int[arline.Length];
                    for (int i = 0; i < arline.Length; i++)
                    {
                    if (arline[i]!="")
                        intline[i] = int.Parse(arline[i]);
                    }
                    retlist.Add(intline);
                
            }
            return retlist;
        }

        public static List<int[]> Create_pre_basis(int[] equation)  //create prebasis for given equation, return list of vectors
        {
            List<int[]> pre_basis = new List<int[]>();
            int i_not_zero = 0;  //placement of first coordinate which not equals zero
            for (int i = 0; i < equation.Length; i++)  //search for i_not_zero
            {
                if (equation[i] != 0)
                {
                    i_not_zero = i;
                    break;
                }
            }
            for (int i = 0; i < equation.Length; i++)  //iterate through equation coefficients
            {
                if (i == i_not_zero)  //skip
                {
                    continue;
                }
                int[] pre_basis_vector_i = new int[equation.Length];  //initialize with zeroes
                pre_basis_vector_i[i_not_zero] = equation[i] * -1;  //put inverted coefficient on place of first not zero
                pre_basis_vector_i[i] = equation[i_not_zero];  //replace coefficient with first not zero
                pre_basis.Add(pre_basis_vector_i);  //for each coefficient add new vector to prebasis
            }
            return pre_basis;
        }

        public static int[] Substitute(int[] equation, List<int[]> pre_basis)  //substitute vectors of prebasis to given equation
        {
            int [] result = new int[pre_basis.Count];  //for each prebasis vector
            for (int i = 0; i < pre_basis.Count; i++)
            {
                int one_vector_result = 0;  //value of substitution of single prebasis vector
                for (int c = 0; c < equation.Length; c++)
                {
                    result[i] += pre_basis[i][c] * equation[c];
                }
            }
            return result;
        }

        public static List<int[]> Multiply_pre_basis(List<int[]> big_pre_basis, List<int[]> small_pre_basis)  //multiplies pre basis from equation gained from substitution and pre basis from initial equation(main prebasis)
        {
            List<int[]> result = new List<int[]>();  //list of vectors. List size equals to size of small_pre_basis(gained from substitution)
            for (int i = 0; i < small_pre_basis.Count; i++)  //small_vector=small_pre_basis[i]
            {
                int[] one_vector_result = new int[big_pre_basis[0].Length];  //result from one vector of substitution prebasis
                for (int i_small_vector = 0; i_small_vector < small_pre_basis[i].Length; i_small_vector++)  //for each coefficient of vector from substituion prebasis
                {
                    if (small_pre_basis[i][i_small_vector] != 0)  //if coefficient not zero
                    {
                        for (int c = 0; c < big_pre_basis[0].Length; c++)
                        {
                            //multiply coefficient of vector from small prebasis(from substitution) with each vector from main prebasis(big), sum vectors
                            one_vector_result[c] +=
                                big_pre_basis[i_small_vector][c] * small_pre_basis[i][i_small_vector];
                        }
                    }
                }
                result.Add(one_vector_result);
            }
            return result;
        }

        static T Reduce<T, U>(Func<U, T, T> func, IEnumerable<U> list, T acc)  //applies function cumulatively to the items of list
        {
            foreach (var i in list)
                acc = func(i, acc);

            return acc;
        }

        static IEnumerable<TResult> Map<T, TResult>(Func<T, TResult> func, IEnumerable<T> list)  //return an iterator that applies function to every item of iterable, yielding the results
        {
            foreach (var i in list)
                yield return func(i);
        }

        static int GCD(int a, int b)  //Greatest Common Divisor
        {
            if (b == 0)
                return a;
            else
                return GCD(b, a % b);
        }

        static List<int[]> Simplify(List<int[]> ar)  //simplifying vectors of matrix
        {
            List<int[]> ar2 = new List<int[]>() { };  //for storing result
            foreach (int[] y in ar)
            {
                int[] y2 = y;
                int d = Reduce(GCD, y, 0);  //searching for greatest common divisor
                if (d != 0 && d != 1)
                {
                    y2 = Map<int, int>(z => z / d, y).ToArray();  //dividing by GCD
                }
                ar2.Add(y2);  //append to answer
            }
            return ar2;
        }

        public static List<int[]> Solv(List<int[]> input_arr)
        {
            List<int[]> pre_basis_main = Create_pre_basis(input_arr[0]);  //create prebasis for the first equation
            for (int Li = 1; Li < input_arr.Count; Li++)  //iterate through other equations
            {
                int[] Y = Substitute(input_arr[Li], pre_basis_main);  //substitute vectors of prebasis to equation Li
                List<int[]> pre_basis_Y = Create_pre_basis(Y);  //create prebasis from result of substitution
                pre_basis_main = Multiply_pre_basis(pre_basis_main, pre_basis_Y);  //get new main prebasis
                pre_basis_main = Simplify(pre_basis_main);  //simplify vectors of prebasis if possible
            }
            return pre_basis_main;
        }

        static void Main(string[] args)
        {
            string inputDirectory = @"C:\Users\dmitr\source\repos\DiophantineZ_sharp\Generator\Input\";
            string outputFile = "_Sharp_beta.csv";

            foreach (string filename in Directory.EnumerateFiles(inputDirectory)) //iterate through all files with inputs in directory
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                string input_size = "";
                List<int[]> red = Read_file(filename);
                for (int i = 0; i < 10; i++)
                {
                    red = Read_file(filename);
                    sw.Start();
                    Solv(red);
                    sw.Stop();
                    input_size = red.Count.ToString() + "x" + red[0].Length.ToString();
                }

                Console.WriteLine(input_size);
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(new System.IO.FileStream(outputFile, FileMode.Append)))
                {
                    file.WriteLine("{0},{1},{2}", red.Count.ToString(), (red[0].Length).ToString(),
                        (sw.ElapsedMilliseconds/10000.0).ToString("F4", CultureInfo.InvariantCulture));
                }
            }

            //string project_root = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory()));
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //string file = project_root + "\\" + "generated.txt";
            //List<int[]> red = Read_file(file);
            //sw.Start();
            //List<int[]> x = Solv(red);
            //sw.Stop();
            //x.ForEach(i => write_arr(i));
            //Console.WriteLine();
            //Console.WriteLine((sw.ElapsedMilliseconds / 100.0).ToString());
            //Console.ReadKey();
        }
    }
}
