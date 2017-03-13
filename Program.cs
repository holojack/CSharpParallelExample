using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


namespace CS474Lab2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("//////////////////////////////////////////////////////////////");
            Console.WriteLine("///// Finding max value of array sequential vs parallel. /////");
            Console.WriteLine("//////////////////////////////////////////////////////////////");
            Console.WriteLine();
            LargestFinder.Run();
            Console.WriteLine();
            Console.WriteLine("////////////////////////////////////////////////////////////////////////");
            Console.WriteLine("///// Finding primes between 1 and a value sequential vs parallel. /////");
            Console.WriteLine("////////////////////////////////////////////////////////////////////////");
            Console.WriteLine();
            PrimeFinder.Run();
            Console.WriteLine();
            Console.WriteLine("//////////////////////////////////");
            Console.WriteLine("///// Testing complete       /////");
            Console.WriteLine("///// Press any key to exit. /////");
            Console.WriteLine("//////////////////////////////////");
            Console.ReadLine();
        }
    }

    class PrimeFinder
    {
        public const int SIZE = System.Int32.MaxValue / 4 - 1;
        private static int[] array = new int[SIZE+1];
        private static int CC = Environment.ProcessorCount;
        private static int count;

        public static void Run()
        {
            count = 0;
            Stopwatch sw = new Stopwatch();
            InitializeArray();
            sw.Start();
            FindPrimesSequentially();
            sw.Stop();
            GetCount();
            Console.WriteLine("Sequential: Found {0} primes between 1 and {1}",count,SIZE);
            Console.WriteLine("Time taken sequentially: {0} milliseconds", sw.ElapsedMilliseconds);
            sw.Reset();
            count = 0;

            Console.WriteLine();

            InitializeArray();
            sw.Start();
            FindPrimesParallel();
            sw.Stop();
            GetCount();
            Console.WriteLine("Parallel: Found {0} primes between 1 and {1}", count, SIZE);
            Console.WriteLine("Time taken in parallel: {0} milliseconds", sw.ElapsedMilliseconds);
            sw.Reset();
        }
        
        static void FindPrimesSequentially()
        {
            int iters = (int) Math.Floor(Math.Sqrt(SIZE));
            for (int i = 2; i <= iters; i++)
            {
                if (array[i] == 1)
                {
                    for (int j = i * 2; j <= SIZE; j += i)
                        array[j] = 0;
                }
            }
        }

        static void FindPrimesParallel()
        {
            int iters = (int)Math.Floor(Math.Sqrt(SIZE));
            Parallel.For(2, CC+2, i =>
             {
                 for (int ii = i; ii < iters; ii += CC)
                 {
                     if (array[ii] == 1)
                     {
                         for (int j = ii * 2; j <= SIZE; j += ii)
                             array[j] = 0;
                     }
                 }
             });
        }

        static void InitializeArray()
        {
            int iters = (int) Math.Ceiling((double)SIZE / CC);

            Parallel.For(0,iters, i =>
            {
                int lMax = CC * (1 + i);
                if(lMax > SIZE)
                {
                    lMax = SIZE;
                }
                for(int ii = CC * i; ii < lMax; ii ++ )
                {
                    array[ii] = 1;
                }
            });
        }

        static void GetCount()
        {
            for (int i = 2; i <= SIZE; i++)
            {
                if (array[i] == 1)
                {
                    count++;
                }
            }
        }
    }

    class LargestFinder
    {
        //Biggest array C# will initialize on windows with default settings
        public const int SIZE = System.Int32.MaxValue / 4;
        private static int CC = Environment.ProcessorCount;

        public static void Run()
        {
            Stopwatch sw = new Stopwatch();

            Console.WriteLine("Array size: {0}", SIZE);

            int[] firstArr = new int[SIZE];
            sw.Start();
            fillArray(firstArr);
            sw.Stop();
            Console.WriteLine("Filling the first array took {0} milliseconds", sw.ElapsedMilliseconds);
            sw.Reset();

            int[] secondArr = new int[SIZE];
            sw.Start();
            fillArrayParrallel(secondArr);
            sw.Stop();
            Console.WriteLine("Filling the second array (Parallel) took {0} milliseconds", sw.ElapsedMilliseconds);
            sw.Reset();

            Console.WriteLine();

            sw.Start();
            int max = findLargestSequentially(firstArr);
            sw.Stop();
            Console.WriteLine("Largest number found in first array sequentially: {0}", max);
            Console.WriteLine("Sequential search took {0} milliseconds", sw.ElapsedMilliseconds);
            sw.Reset();

            Console.WriteLine();

            sw.Start();
            max = findLargestParallelChunks(firstArr, SIZE / CC);
            sw.Stop();
            Console.WriteLine("Largest number found in first array parallel: {0}", max);
            Console.WriteLine("Parallel search took {0} milliseconds", sw.ElapsedMilliseconds);

            Console.WriteLine();

            sw.Start();
            max = findLargestSequentially(secondArr);
            sw.Stop();
            Console.WriteLine("Largest number found in second array sequentially: {0}", max);
            Console.WriteLine("Sequential search took {0} milliseconds", sw.ElapsedMilliseconds);
            sw.Reset();

            Console.WriteLine();

            sw.Start();
            max = findLargestParallelChunks(secondArr, SIZE / CC);
            sw.Stop();
            Console.WriteLine("Largest number found in second array parallel: {0}", max);
            Console.WriteLine("Parallel search took {0} milliseconds", sw.ElapsedMilliseconds);
        }

        /*
         * Use pass in array so initialization and return do not effect benchmarking
         */
        static void fillArray(int[] nums)
        {
            Random rand = new Random();

            for (int i = 0; i < nums.Length; i++)
            {
                nums[i] = rand.Next();
            }
        }

        /*
         * Use pass in array so initialization and return do not effect benchmarking
         */
        static void fillArrayParrallel(int[] nums)
        {
            int chunk = nums.Length / CC;
            Parallel.For(0, CC, index =>
            {
                Random rand = new Random();

                //Get the size of the chunk this thread is responsible for
                int nSize = (index + 1) * chunk;

                for (int i = index * chunk; i < nSize; i++)
                {
                    nums[i] = rand.Next();
                }
            });
        }

        static int findLargestSequentially(int[] nums)
        {
            int largest = nums[0];

            foreach (int x in nums)
            {
                if (x > largest)
                {
                    largest = x;
                }
            }

            return largest;
        }

        static int findLargestParallelChunks(int[] nums, int chunk)
        {
            int largest = nums[0];

            Parallel.For(0, nums.Length / chunk, index =>
            {
                //Get the size of the chunk this thread will search
                int nSize = (index + 1) * chunk;

                int nMax = -1;
                for (int i = index * chunk; i < nSize; i++)
                {
                    if (nums[i] > nMax)
                    {
                        nMax = nums[i];
                    }
                }

                //Locks the global largest more efficiently than lock or Interlocked does.
                Mutex m = new Mutex();
                m.WaitOne();
                if (nMax > largest)
                {
                    largest = nMax;
                }
                //Release global max
                m.ReleaseMutex();
            });

            return largest;
        }
    }
}
