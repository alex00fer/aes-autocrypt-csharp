using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AESAutocrypt
{
    static class Autocrypt
    {
        // Configuration
        const string dirPathRelative = @".\";
        const string aesPathRelative = @".\AESautocrypt";
        const string encryptedExt = ".aes";
        const string metaExt = ".meta";
        const int defaultWipePasses = 1; // Set this to 0 if you are using this on a SSD

        public static void EncryptDirectory()
        {

            Console.WriteLine("Enter the password to encrypt path '{0}' \n",
                Path.GetFullPath(dirPathRelative));

            string password = "";

            while (true)
            {

                password = AskForPassword("New password".PadLeft(20));
                Console.WriteLine("");
                string rpassword = AskForPassword("Repeat password".PadLeft(20));

                if (password == rpassword)
                    break;
                else
                    Console.WriteLine("\n\nPasswords do not match! Try again:\n");
            }

            //Create global meta file
            string globalMetaFile = Path.Combine(aesPathRelative, encryptedExt + metaExt);
            using (FileStream fs = new FileStream(globalMetaFile, FileMode.Create, FileAccess.Write))
            {
                byte[] salt = AESWrapper.GenerateSalt(16);
                fs.Write(salt, 0, salt.Length);
                byte[] hash = AESWrapper.Hash(password, salt, 16);
                fs.Write(hash, 0, hash.Length);
                //fs.Close();
            }

            Console.WriteLine("\n");
            var allfiles = Directory.GetFiles(dirPathRelative);

            int maxConcurrency = 8;
            List<Task> tasks = new List<Task>();
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxConcurrency))
            {

                foreach (string file in allfiles)
                {
                    //if self ignore
                    if (Path.GetFileName(file) == Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location))
                        continue;

                    string fileExt = Path.GetExtension(file);
                    if (fileExt != encryptedExt && fileExt != metaExt)
                    {

                        concurrencySemaphore.Wait();

                        var t = Task.Factory.StartNew(() =>
                        {

                            string filename = Path.GetFileName(file);

                            //Encrypt
                            try
                            {
                                Console.Write("\n>{0}", file);
                                AESWrapper.EncryptFile(file, Path.Combine(aesPathRelative, filename + encryptedExt), password);

                                Console.Write(" [encrypted] ");

                                AESWrapper.WipeFile(file, defaultWipePasses); // Wipe file with X passes (not recommended for SSD)

                                Console.Write(" [wiped] ");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("'{0}' could not be encrypted.", file);
                                Console.WriteLine(e);
                            }
                            finally
                            {
                                concurrencySemaphore.Release();
                            }
                        });
                        tasks.Add(t);
                    }
                }

                Task.WaitAll(tasks.ToArray());
            }

        }

        public static void DecryptDirectory()
        {
            //Load global meta file
            string globalMetaFile = Path.Combine(aesPathRelative, encryptedExt + metaExt);
            byte[] gsalt = new byte[16];
            byte[] ghash = new byte[16];
            using (FileStream fs = new FileStream(globalMetaFile, FileMode.Open, FileAccess.Read))
            {
                fs.Read(gsalt, 0, gsalt.Length);
                fs.Read(ghash, 0, ghash.Length);
            }


            Console.WriteLine("Enter the password to decrypt path '{0}' \n", Path.GetFullPath(dirPathRelative));

            string password = "";
            bool correctPass = false;
            do
            {

                password = AskForPassword("Password".PadLeft(20));

                if (AESWrapper.Hash(password, gsalt, 16).SequenceEqual(ghash))
                    correctPass = true;
                else
                    Console.WriteLine("\nWrong password\n");

            } while (!correctPass);


            var allfiles = Directory.GetFiles(aesPathRelative);


            int maxConcurrency = 8;
            List<Task> tasks = new List<Task>();
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxConcurrency))
            {

                foreach (string file in allfiles)
                {

                    string fileExt = Path.GetExtension(file);
                    if (fileExt == encryptedExt)
                    {
                        //Decrypt
                        string filename = Path.GetFileName(file);
                        string outFile = Path.Combine(dirPathRelative, filename.Remove(filename.Length - encryptedExt.Length));

                        concurrencySemaphore.Wait();

                        var t = Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                Console.Write("\n>{0}", file);
                                AESWrapper.DecryptFile(file, outFile, password);
                                Console.Write(" [decrypted] ");

                                using (FileStream metafile = new FileStream(file + metaExt, FileMode.Open, FileAccess.Write))
                                {
                                    metafile.Position = 24;
                                    long date = File.GetLastWriteTime(outFile).ToBinary();
                                    byte[] dateb = BitConverter.GetBytes(date);
                                    metafile.Write(dateb, 0, dateb.Length);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("'{0}' could not be decrypted.", file);
                                File.Delete(outFile);
                                Console.WriteLine(e);
                            }
                            finally
                            {
                                concurrencySemaphore.Release();
                            }
                        });

                        tasks.Add(t);
                    }
                }

                Task.WaitAll(tasks.ToArray());

            }
        }


        public static void DeleteDecrypted()
        {
            object passLocker = new object();
            string password = null;
            var allfiles = Directory.GetFiles(dirPathRelative);

            int maxConcurrency = 8;
            List<Task> tasks = new List<Task>();
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxConcurrency))
            {

                foreach (string file in allfiles)
                {

                    string fileExt = Path.GetExtension(file);
                    if (fileExt != encryptedExt && fileExt != metaExt)
                    {
                        string filename = Path.GetFileName(file);

                        if (Path.GetFileName(file) == Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location))
                            continue;

                        string encryptedFile = Path.Combine(aesPathRelative, filename + encryptedExt);
                        bool encryptedFileExists = File.Exists(encryptedFile);

                        long metaDate = 0;

                        concurrencySemaphore.Wait();

                        var t = Task.Factory.StartNew(() =>
                        {
                            if (encryptedFileExists)
                            {
                                using (FileStream metafile = new FileStream(encryptedFile + metaExt, FileMode.Open, FileAccess.Read))
                                {
                                    metafile.Position = 24;
                                    byte[] dateb = new byte[8];
                                    metafile.Read(dateb, 0, dateb.Length);
                                    metaDate = BitConverter.ToInt64(dateb, 0);

                                }
                            }

                            if (encryptedFileExists && File.GetLastWriteTime(file).ToBinary() == metaDate)
                            {
                                lock (passLocker)
                                {
                                    Console.WriteLine("Wiping {0}", file);
                                }
                                AESWrapper.WipeFile(file, 1);
                            }
                            else
                            {
                                lock (passLocker)
                                {
                                    //New file -> encrypt
                                    if (password == null)
                                    {
                                        string globalMetaFile = Path.Combine(aesPathRelative, encryptedExt + metaExt);
                                        byte[] gsalt = new byte[16];
                                        byte[] ghash = new byte[16];
                                        using (FileStream fs = new FileStream(globalMetaFile, FileMode.Open, FileAccess.Read))
                                        {
                                            fs.Read(gsalt, 0, gsalt.Length);
                                            fs.Read(ghash, 0, ghash.Length);
                                        }


                                        Console.WriteLine("\nEnter the password to encrypt new file'{0}' \n", filename);

                                        password = "";
                                        bool correctPass = false;
                                        do
                                        {

                                            password = AskForPassword("Password".PadLeft(20));

                                            if (AESWrapper.Hash(password, gsalt, 16).SequenceEqual(ghash))
                                                correctPass = true;
                                            else
                                                Console.WriteLine("\n\nWrong password\n");

                                        } while (!correctPass);
                                    }
                                }

                                if (fileExt != encryptedExt && fileExt != metaExt)
                                {
                                    //Encrypt
                                    try
                                    {
                                        Console.Write("\n>{0}", file);
                                        AESWrapper.EncryptFile(file, Path.Combine(aesPathRelative, filename + encryptedExt), password);

                                        Console.Write(" [encrypted] ");

                                        AESWrapper.WipeFile(file, 1);

                                        Console.Write(" [wiped] ");
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("'{0}' could not be encrypted.", file);
                                        Console.WriteLine(e);
                                    }


                                }
                            }
                            concurrencySemaphore.Release();
                        });

                        tasks.Add(t);
                    }
                }

                Task.WaitAll(tasks.ToArray());
            }
        }


        static string AskForPassword(string text)
        {
            Console.Write(text + ": ");
            string password = "";

            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.F1)
                {
                    Console.WriteLine("F1");
                }
                else
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }

            } while (true);

            return password;
        }
    }
}