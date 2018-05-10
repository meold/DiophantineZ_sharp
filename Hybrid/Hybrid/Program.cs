using Hybridizer.Runtime.CUDAImports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hybrid
{
    class Program
    {
        [EntryPoint("run")]
        public static void Run(int N, int[] a, int[] b)
        {
            Parallel.For(0, N, i => { a[i] += b[i]; });
        }

        static void Main(string[] args)
        {
            int[] a = { 1, 2, 3, 4, 5 };
            int[] b = { 10, 20, 30, 40, 50 };

            cudaDeviceProp prop;
            cuda.GetDeviceProperties(out prop, 0);
            //if .SetDistrib is not used, the default is .SetDistrib(prop.multiProcessorCount * 16, 128)
            HybRunner runner = HybRunner.Cuda("Hybrid_CUDA.dll");

            // create a wrapper object to call GPU methods instead of C#
            dynamic wrapped = runner.Wrap(new Program());

            wrapped.Run(5, a, b);

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(new System.IO.FileStream("sharpZ.txt", FileMode.Append)))
            {
                file.WriteLine("just a test");
                file.WriteLine(a[0].ToString());
            }

            Console.Out.WriteLine("DONE");

           
        }
    }
}
