using System;
using System.Diagnostics;

namespace PasswordCrackerServiceConsumer
{
    class Program
    {
        /// <summary>
        /// The main method of the program.
        /// </summary>
        static void Main()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Cracking cracker = new Cracking();
            cracker.RunCracking();
            Console.WriteLine("Time elapsed: {0}", sw.Elapsed);
            Console.ReadLine();
        }
    }
}