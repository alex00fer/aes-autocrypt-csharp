using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AESAutocrypt
{
    class AppStart
    {
        // Configuration
        const string dirPathRelative = @".\";
        const string aesPathRelative = @".\AESautocrypt";
        const string encryptedExt = ".aes";
        const string metaExt = ".meta";
        // State
        static bool driveExists = false;


        static void Main(string[] args)
        {
            // Create the needed working directory if it doesn't exist yet
            Directory.CreateDirectory(aesPathRelative);

            while (Menu());
        }

        static bool Menu()
        {

            Console.Clear();

            PrintLogo();

            Console.WriteLine("");
            Console.WriteLine("");

            if (File.Exists(Path.Combine(aesPathRelative, encryptedExt + metaExt)))
                driveExists = true;

            if (!driveExists)
            {
                Console.Write("  [F1] Encrypt path (first time)  ");
                Console.Write("  [ESC] Exit".PadLeft(40));
            }
            else
            {
                Console.Write("  [F4] Decrypt path  ");
                Console.Write("  [F5] Encrypt path  ");
                Console.Write("  [ESC] Exit");
            }
            Console.WriteLine("");
            Console.WriteLine("");



            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.F1 && !driveExists)
                {
                    Autocrypt.EncryptDirectory();
                    Menu();
                }
                else if (key.Key == ConsoleKey.F5 && driveExists)
                {
                    Autocrypt.DeleteDecrypted();
                    Menu();
                }
                else if (key.Key == ConsoleKey.F4 && driveExists)
                {
                    Autocrypt.DecryptDirectory();
                    Menu();
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    return false; // EXIT
                }
                else
                    return true; // SHOW MENU AGAIN
            }

        }


        static void PrintLogo()
        {
            Console.WriteLine("          _    _ _______ ____   _____ _______     _______ _______ ");
            Console.WriteLine("     /\\  | |  | |__   __/ __ \\ / ____|  __ \\ \\   / /  __ \\__   __|");
            Console.WriteLine("    /  \\ | |  | |  | | | |  | | |    | |__) \\ \\_/ /| |__) | | |   ");
            Console.WriteLine("   / /\\ \\| |  | |  | | | |  | | |    |  _  / \\   / |  ___/  | |   ");
            Console.WriteLine("  / ____ \\ |__| |  | | | |__| | |____| | \\ \\  | |  | |      | |   ");
            Console.WriteLine(" /_/    \\_\\____/   |_|  \\____/ \\_____|_|  \\_\\ |_|  |_|      |_|   Ver. 0.1MD");
            Console.WriteLine("");
        }
    }
}
