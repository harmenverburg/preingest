using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Utilities
{
    public static class ChecksumHelper
    {
        public static string CreateMD5Checksum(System.IO.FileInfo currentFile)
        {
            StringBuilder sb = new StringBuilder();

            string path = currentFile.FullName;
            bool isTooLong = currentFile.FullName.Length > 245;
            if (isTooLong)
            {
                string tempFile = System.IO.Path.GetTempFileName();
                currentFile.CopyTo(tempFile, true);
                path = tempFile;
            }

            if (currentFile == null)
                throw new ArgumentNullException("Invalid parameter passed! Null reference detected.");

            using (System.IO.FileStream file = new System.IO.FileStream(path, System.IO.FileMode.Open))
            {
                using (MD5 md5 = new MD5CryptoServiceProvider())
                {
                    byte[] retVal = md5.ComputeHash(file);

                    for (int i = 0; i < retVal.Length; i++)
                        sb.Append(retVal[i].ToString("x2"));
                }
            }

            if (isTooLong)
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch { }
            }

            return sb.ToString();
        }

        public static string CreateSHA1Checksum(System.IO.FileInfo currentFile)
        {
            StringBuilder sb = new StringBuilder();

            if (currentFile == null)
                throw new ArgumentNullException("Invalid parameter passed! Null reference detected.");

            string path = currentFile.FullName;
            bool isTooLong = currentFile.FullName.Length > 245;
            if (isTooLong)
            {
                string tempFile = System.IO.Path.GetTempFileName();
                currentFile.CopyTo(tempFile, true);
                path = tempFile;
            }

            using (System.IO.FileStream file = new System.IO.FileStream(path, System.IO.FileMode.Open))
            {
                using (SHA1 sha1 = new SHA1CryptoServiceProvider())
                {
                    byte[] retVal = sha1.ComputeHash(file);

                    for (int i = 0; i < retVal.Length; i++)
                        sb.Append(retVal[i].ToString("x2"));
                }
            }

            if (isTooLong)
            {
                try
                {
                   System.IO.File.Delete(path);
                }
                catch { }
            }

            return sb.ToString();
        }

        public static string CreateSHA512Checksum(System.IO.FileInfo currentFile)
        {
            StringBuilder sb = new StringBuilder();

            if (currentFile == null)
                throw new ArgumentNullException("Invalid parameter passed! Null reference detected.");

            string path = currentFile.FullName;
            bool isTooLong = currentFile.FullName.Length > 245;
            if (isTooLong)
            {
                string tempFile = System.IO.Path.GetTempFileName();
                currentFile.CopyTo(tempFile, true);
                path = tempFile;
            }

            using (System.IO.FileStream file = new System.IO.FileStream(path, System.IO.FileMode.Open))
            {
                using (SHA512 sha1 = new SHA512Managed())
                {
                    byte[] retVal = sha1.ComputeHash(file);

                    for (int i = 0; i < retVal.Length; i++)
                        sb.Append(retVal[i].ToString("x2"));
                }
            }

            if (isTooLong)
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch { }
            }

            return sb.ToString();
        }

        public static string CreateSHA256Checksum(System.IO.FileInfo currentFile)
        {
            StringBuilder sb = new StringBuilder();

            if (currentFile == null)
                throw new ArgumentNullException("Invalid parameter passed! Null reference detected.");

            string path = currentFile.FullName;
            bool isTooLong = currentFile.FullName.Length > 245;
            if (isTooLong)
            {
                string tempFile = System.IO.Path.GetTempFileName();
                currentFile.CopyTo(tempFile, true);
                path = tempFile;
            }

            using (System.IO.FileStream file = new System.IO.FileStream(path, System.IO.FileMode.Open))
            {
                using (SHA256 sha1 = new SHA256Managed())
                {
                    byte[] retVal = sha1.ComputeHash(file);

                    for (int i = 0; i < retVal.Length; i++)
                        sb.Append(retVal[i].ToString("x2"));
                }
            }

            if (isTooLong)
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch { }
            }

            return sb.ToString();
        }
    }
}
