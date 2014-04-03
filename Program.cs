using System;

namespace PasswordCrackerServiceConsumer
{
    class Program
    {
        static void Main()
        {
            Cracking cracker = new Cracking();
            cracker.RunCracking();
            Console.ReadLine();
        }
    }
}