using System;
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
        public static void Create_pre_basis(IntResidentArray equation, IntResidentArray pre_basis_main, int N)  //create prebasis for given equation, return list of vectors
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
                if (i > i_not_zero) //skip
                {
                    for (int p = 0; p < N; p++)
                    {
                        pre_basis_main[(i-1) * N + p] = 0;
                    }
                    pre_basis_main[(i-1)*N + i_not_zero] =
                        equation[i] * -1; //put inverted coefficient on place of first not zero
                    pre_basis_main[(i - 1) * N + i] = equation[i_not_zero]; //replace coefficient with first not zero
                }
                else if (i < i_not_zero)
                {
                    for (int p = 0; p < N; p++)
                    {
                        pre_basis_main[i * N + p] = 0;
                    }
                    pre_basis_main[i*N + i_not_zero] =
                        equation[i] * -1; //put inverted coefficient on place of first not zero
                    pre_basis_main[i*N+i] = equation[i_not_zero]; //replace coefficient with first not zero
                }
            });
        }

        [EntryPoint]
        public static void Substitute(int[] equation, IntResidentArray pre_basis, IntResidentArray result, int equationLength, int pre_basisLengthAxis0)  //substitute vectors of prebasis to given equation
        {
            //for each prebasis vector
            Parallel.For(0, pre_basisLengthAxis0, (i) =>
            {
                result[i] = 0;
                for (int c = 0; c < equationLength; c++)
                {
                    result[i] += pre_basis[i*equationLength + c] * equation[c];
                }
            });
        }

        [EntryPoint]
        public static void Multiply_pre_basis(IntResidentArray big_pre_basis, IntResidentArray small_pre_basis, IntResidentArray result, int big_pre_basisLengthAxis1, int small_pre_basisLengthAxis0, int small_pre_basisLengthAxis1)  //multiplies pre basis from equation gained from substitution and pre basis from initial equation(main prebasis)
        {
            Parallel.For(0, small_pre_basisLengthAxis0, (i) => //small_vector=small_pre_basis[i]
            {
                for (int k = 0; k < big_pre_basisLengthAxis1; k++)
                {
                    result[i * big_pre_basisLengthAxis1 + k] = 0;
                }
                for (int i_small_vector = 0;
                    i_small_vector < small_pre_basisLengthAxis1;
                    i_small_vector++) //for each coefficient of vector from substituion prebasis
                {
                    if (small_pre_basis[i*small_pre_basisLengthAxis1 + i_small_vector] != 0) //if coefficient not zero
                    {
                        for (int c = 0; c < big_pre_basisLengthAxis1; c++)
                        {
                            //multiply coefficient of vector from small prebasis(from substitution) with each vector from main prebasis(big), sum vectors
                            result[i*big_pre_basisLengthAxis1 + c] += 
                                big_pre_basis[i_small_vector*big_pre_basisLengthAxis1 + c] * small_pre_basis[i*small_pre_basisLengthAxis1 + i_small_vector];
                        }
                    }
                }
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

        static void Find_gcds(IntResidentArray ar, int pre_basisLengthAxis0, int equationLength, IntResidentArray gcds)
        {
            for (int i = 0; i < pre_basisLengthAxis0; i++)
            {
                int[] row = new int[equationLength];
                for (int j = 0; j < equationLength; j++)
                {
                    row[j] = ar[i*equationLength + j];
                }
                gcds[i] = Reduce(GCD, row, 0);
            }
        }

        [EntryPoint]
        static void Simplify(IntResidentArray ar, IntResidentArray gcds, int arLengthAxis0, int arLengthAxis1) //simplifying vectors of matrix
        {
            Parallel.For(0, arLengthAxis0, (row) =>
            {
                int d = gcds[row];
                if (d != 0 && d != 1)
                {
                    for (int col = 0; col < arLengthAxis1; col++)
                    {
                        ar[row*arLengthAxis1 + col] /= d;
                    }
                }
            });
        }

        public static IntResidentArray Solv(List<int[]> input_arr)
        {
            cudaDeviceProp prop;
            cuda.GetDeviceProperties(out prop, 0);
            //if .SetDistrib is not used, the default is .SetDistrib(prop.multiProcessorCount * 16, 128)
            HybRunner runner = HybRunner.Cuda("Hybrid_CUDA.dll").SetDistrib(prop.multiProcessorCount * 16, 256);

            // create a wrapper object to call GPU methods instead of C#
            dynamic wrapper = runner.Wrap(new Program());

            IntResidentArray pre_basis_main = new IntResidentArray(input_arr[0].Length * (input_arr[0].Length - 1));
            //int[] pre_basis_main = new int[(input_arr[0].Length - 1)*input_arr[0].Length];  //pre_basis is flattened array now


            //int threadsperblock = 256;
            //int blockspergrid = (int)Math.Ceiling((double)(input_arr[0].Length - 1) / threadsperblock);

            int pre_basisLengthAxis0 = input_arr[0].Length - 1;
            int equationLength = input_arr[0].Length;
            IntResidentArray inputArray0 = new IntResidentArray(equationLength);
            inputArray0.RefreshHost();
            for (int j = 0; j < equationLength; j++)
            { inputArray0[j] = input_arr[0][j]; }

            inputArray0.RefreshDevice();
            pre_basis_main.RefreshDevice();
            wrapper.Create_pre_basis(inputArray0, pre_basis_main, equationLength);  //create prebasis for the first equation
            for (int Li = 1; Li < input_arr.Count; Li++)  //iterate through other equations
            {
                IntResidentArray substitution_result = new IntResidentArray(pre_basisLengthAxis0);
                
                //substitution_result.RefreshDevice();
                wrapper.Substitute(input_arr[Li], pre_basis_main, substitution_result, equationLength, pre_basisLengthAxis0);  //substitute vectors of prebasis to equation Li

                IntResidentArray pre_basis_Y = new IntResidentArray((pre_basisLengthAxis0 - 1)*pre_basisLengthAxis0);  //also flattened array

                //substitution_result.RefreshHost();
                //int[] s2 = new int[pre_basisLengthAxis0];
                //for (int k = 0; k < pre_basisLengthAxis0; k++)
                //{
                //    s2[k] = substitution_result[k];
                //}

                pre_basis_Y.RefreshDevice();
                //substitution_result.RefreshDevice();
                wrapper.Create_pre_basis(substitution_result, pre_basis_Y, pre_basisLengthAxis0);  //create prebasis from result of substitution

                IntResidentArray mult_result = new IntResidentArray((pre_basisLengthAxis0-1)*equationLength);

                //pre_basis_main.RefreshHost();
                mult_result.RefreshDevice();
                //pre_basis_Y.RefreshHost();
                //int[] kar = new int[pre_basisLengthAxis0 * 2];
                //for (int k = 0; k < pre_basisLengthAxis0 * 2; k++)
                //{
                //    kar[k] = pre_basis_Y[k];
                //}
                wrapper.Multiply_pre_basis(pre_basis_main, pre_basis_Y, mult_result, equationLength, pre_basisLengthAxis0-1, pre_basisLengthAxis0);  //get new main prebasis

                pre_basis_main = mult_result;
                pre_basisLengthAxis0 -= 1;

                pre_basis_main.RefreshHost();
                IntResidentArray gcds = new IntResidentArray(pre_basisLengthAxis0);
                Find_gcds(pre_basis_main, pre_basisLengthAxis0 , equationLength, gcds);
                gcds.RefreshDevice();

                pre_basis_main.RefreshDevice();
                wrapper.Simplify(pre_basis_main, gcds, pre_basisLengthAxis0, equationLength);  //simplify vectors of prebasis if possible
            }

            pre_basis_main.RefreshHost();
            return pre_basis_main;
        }

        static void Main(string[] args)
        {

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(new System.IO.FileStream("sharpZ.txt", FileMode.Append)))
            {
                file.WriteLine("just a test0");
            }

            



            foreach (string filename in Directory.EnumerateFiles(@"C:\Users\dmitr\Documents\DiophantineZ\Input\")) //iterate through all files with inputs in directory
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                string input_size = "";
                List<int[]> red = Read_file(filename);
                IntResidentArray solution;
                for (int i = 0; i < 10; i++)
                {
                    red = Read_file(filename);
                    sw.Start();
                    Solv(red);
                    sw.Stop();
                    input_size = red.Count.ToString() + "x" + red[0].Length.ToString();
                }

                Console.Out.WriteLine(input_size);
                
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(new System.IO.FileStream("CUDAsharpZ.csv", FileMode.Append)))
                {
                    solution = Solv(red);
                    for (int line = 0; line < solution.Count / red[0].Length; line++)
                    {
                        for (int c = 0; c < red[0].Length; c++)
                        {
                            file.Write(solution[line * red[0].Length + c].ToString() + ", ");
                        }

                        file.WriteLine();
                    }


                    file.WriteLine("{0},{1},{2}", red.Count.ToString(), (red[0].Length).ToString(),
                        (sw.ElapsedMilliseconds / 10000.0).ToString("F4", CultureInfo.InvariantCulture));
                }
                Console.Out.WriteLine("DONE");
            }

        }
    }
}
