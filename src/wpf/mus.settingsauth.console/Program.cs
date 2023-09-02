using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace mus.settingsauth.console
{
    internal class Program
    {
        protected static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        static void Main(string[] args)
        {
            Console.Write("Enter your password: ");
            string password = Console.ReadLine();

            string hashedPassword = ComputeSha256Hash(password);
            Console.WriteLine(hashedPassword);
            Console.ReadLine();
        }
    }
}
