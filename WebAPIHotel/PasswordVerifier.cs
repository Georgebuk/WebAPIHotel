using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace WebAPIHotel
{
    public static class PasswordVerifier
    {
        public static bool Verify(string enteredPass, string savedPassword)
        {
            bool verified = true;
            byte[] hashBytes = Convert.FromBase64String(savedPassword);

            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            var pbkdf2 = new Rfc2898DeriveBytes(enteredPass, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            for (int i = 0; i < 20; i++)
                if (hashBytes[i + 16] != hash[i])
                    verified = false;

            return verified;
        }
    }
}