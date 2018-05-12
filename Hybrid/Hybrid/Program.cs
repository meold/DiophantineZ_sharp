﻿using System;
using Hybridizer.Runtime.CUDAImports;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hybrid
{
    class Program
    {
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
                    if (arline[i] != "")
                        intline[i] = int.Parse(arline[i]);
                }
                retlist.Add(intline);

            }
            return retlist;
        }

        [EntryPoint]
        public static void Create_pre_basis(int[] equation, ref int[][] pre_basis_main, int N, int[] a)  //create prebasis for given equation, return list of vectors
        {
            int i_not_zero = 0;  //placement of first coordinate which not equals zero
            for (int i = 0; i < N; i++)  //search for i_not_zero
            {
                if (equation[i] != 0)
                {
                    i_not_zero = i;
                    break;
                }
            }
            Parallel.For(0, N, (i) =>  //iterate through equation coefficients
            {
                int[] pre_basis_vector_i = new int[equation.Length]; //initialize with zeroes
                if (i > i_not_zero) //skip
                {
                    pre_basis_vector_i[i_not_zero] =
                        equation[i] * -1; //put inverted coefficient on place of first not zero
                    pre_basis_vector_i[i] = equation[i_not_zero]; //replace coefficient with first not zero
                    pre_basis_main[i-1] = pre_basis_vector_i; //for each coefficient add new vector to prebasis
                    a[i] = 5;
                }
                else if (i < i_not_zero)
                {
                    pre_basis_vector_i[i_not_zero] =
                        equation[i] * -1; //put inverted coefficient on place of first not zero
                    pre_basis_vector_i[i] = equation[i_not_zero]; //replace coefficient with first not zero
                    pre_basis_main[i] = pre_basis_vector_i; //for each coefficient add new vector to prebasis
                    a[i] = 5;
                }
            });
        }

        [EntryPoint]
        public static void Substitute(int[] equation, int[][] pre_basis, int[] result, int equationLength, int pre_basisLength)  //substitute vectors of prebasis to given equation
        {
            //for each prebasis vector
            Parallel.For(0, pre_basisLength, (i) =>
            {
                int one_vector_result = 0; //value of substitution of single prebasis vector
                for (int c = 0; c < equationLength; c++)
                {
                    result[i] += pre_basis[i][c] * equation[c];
                }
            });
        }

        [EntryPoint]
        public static void Multiply_pre_basis(int[][] big_pre_basis, int[][] small_pre_basis, int[][] result, int big_pre_basis0Length, int small_pre_basisLength, int small_pre_basis0Lenth)  //multiplies pre basis from equation gained from substitution and pre basis from initial equation(main prebasis)
        {
            Parallel.For(0, small_pre_basisLength, (i) => //small_vector=small_pre_basis[i]
            {
                int[] one_vector_result =
                    new int[big_pre_basis0Length]; //result from one vector of substitution prebasis
                for (int i_small_vector = 0;
                    i_small_vector < small_pre_basis0Lenth;
                    i_small_vector++) //for each coefficient of vector from substituion prebasis
                {
                    if (small_pre_basis[i][i_small_vector] != 0) //if coefficient not zero
                    {
                        for (int c = 0; c < big_pre_basis0Length; c++)
                        {
                            //multiply coefficient of vector from small prebasis(from substitution) with each vector from main prebasis(big), sum vectors
                            one_vector_result[c] +=
                                big_pre_basis[i_small_vector][c] * small_pre_basis[i][i_small_vector];
                        }
                    }
                }

                result[i] = one_vector_result;
            });
        }

        static T Reduce<T, U>(Func<U, T, T> func, IEnumerable<U> list, T acc)  //applies function cumulatively to the items of list
        {
            foreach (var i in list)
                acc = func(i, acc);

            return acc;
        }

        static int GCD(int a, int b)  //Greatest Common Divisor
        {
            if (b == 0)
                return a;
            else
                return GCD(b, a % b);
        }

        static int[] Find_gcds(int[][] ar)
        {
            int[] gcds = new int[ar.GetLength(0)];
            for (int i = 0; i < ar.GetLength(0); i++)
            {
                gcds[i] = Reduce(GCD, ar[i], 0);
            }

            return gcds;
        }

        [EntryPoint]
        static void Simplify(int[][] ar, int[] gcds, int arLength, int ar0Length) //simplifying vectors of matrix
        {
            Parallel.For(0, arLength, (row) =>
            {
                int d = gcds[row];
                if (d != 0 && d != 1)
                {
                    for (int col = 0; col < ar0Length; col++)
                    {
                        ar[row][col] /= d;
                    }
                }
            });
        }

        public static int[][] Solv(List<int[]> input_arr)
        {
            cudaDeviceProp prop;
            cuda.GetDeviceProperties(out prop, 0);
            //if .SetDistrib is not used, the default is .SetDistrib(prop.multiProcessorCount * 16, 128)
            HybRunner runner = HybRunner.Cuda("Hybrid_CUDA.dll");

            // create a wrapper object to call GPU methods instead of C#
            dynamic wrapper = runner.Wrap(new Program());

            //IntResidentArray pre_basis_main = new IntResidentArray(input_arr[0].Length * (input_arr[0].Length - 1));
            int[][] pre_basis_main = new int[input_arr[0].Length - 1][];
            int[] a = new int[input_arr[0].Length - 1];


            //int threadsperblock = 256;
            //int blockspergrid = (int)Math.Ceiling((double)(input_arr[0].Length - 1) / threadsperblock);

            int N = input_arr[0].Length;
            wrapper.Create_pre_basis(input_arr[0], pre_basis_main, N, a);  //create prebasis for the first equation
            for (int Li = 1; Li < input_arr.Count; Li++)  //iterate through other equations
            {
                int[] substitution_result = new int[pre_basis_main.GetLength(0)];
                int[] next_equation = input_arr[Li];

                int equationLength = input_arr[Li].Length;
                int pre_basisLength = pre_basis_main.GetLength(0);
                wrapper.Substitute(input_arr[Li], pre_basis_main, substitution_result, equationLength, pre_basisLength);  //substitute vectors of prebasis to equation Li

                int[][] pre_basis_Y = new int[substitution_result.Length - 1][];

                N = substitution_result.Length;
                wrapper.Create_pre_basis(substitution_result, pre_basis_Y, N);  //create prebasis from result of substitution

                int[][] mult_result = new int[pre_basis_Y.GetLength(0)][];

                int pre_basis_main0Length = pre_basis_main[0].Length;
                int pre_basis_YLength = pre_basis_Y.GetLength(0);
                int pre_basis_Y0Length = pre_basis_Y[0].Length;
                wrapper.Multiply_pre_basis(pre_basis_main, pre_basis_Y, mult_result, pre_basis_main0Length, pre_basis_YLength, pre_basis_Y0Length);  //get new main prebasis

                pre_basis_main = mult_result;

                int[] gcds = Find_gcds(pre_basis_main);

                pre_basisLength = pre_basis_main.GetLength(0);
                pre_basis_main0Length = pre_basis_main[0].Length;
                wrapper.Simplify(pre_basis_main, gcds, pre_basisLength, pre_basis_main0Length);  //simplify vectors of prebasis if possible
            }
            return pre_basis_main;
        }

        static void Main(string[] args)
        {

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(new System.IO.FileStream("sharpZ.txt", FileMode.Append)))
            {
                file.WriteLine("just a test0");
            }

            Console.Out.WriteLine("DONE");



            foreach (string filename in Directory.EnumerateFiles(@"C:\Users\dmitr\Documents\DiophantineZ\Input\")) //iterate through all files with inputs in directory
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                string input_size = "";
                List<int[]> red = Read_file(filename);
                int[][] solution;
                for (int i = 0; i < 10; i++)
                {
                    red = Read_file(filename);
                    sw.Start();
                    solution = Solv(red);
                    sw.Stop();
                    input_size = red.Count.ToString() + "x" + red[0].Length.ToString();
                }

                Console.WriteLine(input_size);
                solution = Solv(red);
                foreach (int[] line in solution)
                {
                    foreach (int c in line)
                    {
                        Console.Write(c.ToString() + ", ");
                    }

                    Console.WriteLine();
                }
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(new System.IO.FileStream("sharpZ.csv", FileMode.Append)))
                {
                    file.WriteLine("{0},{1},{2}", red.Count.ToString(), (red[0].Length).ToString(),
                        (sw.ElapsedMilliseconds / 10000.0).ToString("F4", CultureInfo.InvariantCulture));
                }
            }

        }
    }
}
