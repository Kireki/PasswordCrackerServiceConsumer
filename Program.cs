using System;

namespace PasswordCrackerServiceConsumer
{
    class Program
    {
        /// <summary>
        /// The main method of the program.
        /// </summary>
        static void Main()
        {
            Cracking cracker = new Cracking();
            cracker.RunCracking();
            Console.ReadLine();
        }
    }
}