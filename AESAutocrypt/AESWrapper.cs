using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AESAutocrypt
{
    public static class AESWrapper
    {

        private static RNGCryptoServiceProvider cryptoRan = new RNGCryptoServiceProvider();
        private const int iterations = 7000;

        private static byte[] staticSalt = { 0xc4, 0x0a, 0x7f, 0x4f,
                                             0xa2, 0xc0, 0x92, 0x37 };


        public static void DecryptFile(string fileIn, string fileOut, string password)
        {
            AesManaged aes = new AesManaged();

            aes.BlockSize = aes.LegalBlockSizes[0].MaxSize;
            aes.KeySize = aes.LegalKeySizes[0].MaxSize;

            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;


            // Load meta file
            byte[] salt = new byte[8];
            byte[] iv = new byte[aes.BlockSize / 8];

            using (FileStream metafile = new FileStream(fileIn + ".meta", FileMode.Open, FileAccess.Read))
            {
                metafile.Read(salt, 0, salt.Length);
                metafile.Read(iv, 0, iv.Length);
            }
            var rfc2898 = new Rfc2898DeriveBytes(password, salt, iterations);
            //End

            aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
            aes.IV = iv;

            ICryptoTransform transform = aes.CreateDecryptor();

            try
            {

                using (FileStream destination = new FileStream(fileOut, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(destination, transform, CryptoStreamMode.Write))
                    {
                        using (FileStream source = new FileStream(fileIn, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            source.CopyTo(cryptoStream);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public static void EncryptFile(string fileIn, string fileOut, string password)
        {
            AesManaged aes = new AesManaged();

            aes.BlockSize = aes.LegalBlockSizes[0].MaxSize;
            aes.KeySize = aes.LegalKeySizes[0].MaxSize;

            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            byte[] salt = GenerateSalt(8);
            var rfc2898 = new Rfc2898DeriveBytes(password, salt, iterations);

            aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
            aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);

            using (FileStream metafile = new FileStream(fileOut + ".meta", FileMode.Create, FileAccess.Write))
            {
                metafile.Write(salt, 0, salt.Length);
                metafile.Write(aes.IV, 0, aes.BlockSize / 8);
            }

            ICryptoTransform transform = aes.CreateEncryptor();

            try
            {

                using (FileStream destination = new FileStream(fileOut, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(destination, transform, CryptoStreamMode.Write))
                    {
                        using (FileStream source = new FileStream(fileIn, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            source.CopyTo(cryptoStream);
                            cryptoStream.FlushFinalBlock();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static bool WipeFile(string filename, int passes = 3)
        {

            if (File.Exists(filename))
            {
                // Set the files attributes to normal in case it's read-only.
                File.SetAttributes(filename, FileAttributes.Normal);

                FileStream inputStream = new FileStream(filename, FileMode.Open);

                long sectors = inputStream.Length / 4096;

                byte[] dummyBuffer = new byte[4096];


                for (int currentPass = 0; currentPass < passes; currentPass++)
                {
                    // Go to the beginning of the stream
                    inputStream.Position = 0;

                    // Loop all full sectors
                    for (int sectorsWritten = 0; sectorsWritten < sectors; sectorsWritten++)
                    {
                        cryptoRan.GetBytes(dummyBuffer);
                        inputStream.Write(dummyBuffer, 0, dummyBuffer.Length);
                    }
                    // Last incomplete sector
                    cryptoRan.GetBytes(dummyBuffer);
                    inputStream.Write(dummyBuffer, 0, (int)(inputStream.Length - inputStream.Position));
                }

                // Truncate the file to 0 bytes.
                // This will hide the original file-length if you try to recover the file.
                inputStream.SetLength(0);
                inputStream.Close();

                //Change name
                for (int i = 0; i < 5; i++)
                {
                    string oldname = filename;
                    filename = Guid.NewGuid().ToString("N");
                    File.Move(oldname, filename);
                }

                // As an extra precaution I change the dates of the file so the
                // original dates are hidden if you try to recover the file.
                DateTime dt = new DateTime(2037, 1, 1, 0, 0, 0);
                File.SetCreationTime(filename, dt);
                File.SetLastAccessTime(filename, dt);
                File.SetLastWriteTime(filename, dt);

                File.SetCreationTimeUtc(filename, dt);
                File.SetLastAccessTimeUtc(filename, dt);
                File.SetLastWriteTimeUtc(filename, dt);

                // Finally, delete the file
                File.Delete(filename);

                return true;
            }


            return false;
        }



        public static byte[] GenerateSalt(int saltLength)
        {
            var salt = new byte[saltLength];
            cryptoRan.GetNonZeroBytes(salt);

            return salt;
        }

        public static byte[] Hash(string password, byte[] salt, int hashBytes)
        {
            var rfc2898 = new Rfc2898DeriveBytes(password, salt, iterations);
            return rfc2898.GetBytes(hashBytes);
        }
    }
}
