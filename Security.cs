using System;
using System.Security.Cryptography;
using System.Text;

namespace RailTicketSystem 
{
    public static class Security
    {
        public static string GenerateSalt()
        {
            byte[] bytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider()) //SALT 생성 16바이트 
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        public static string HashPassword(string password, string salt)
        {
            var sha = SHA256.Create(); //SHA256 알고리즘 객체를 생성
            var combined = Encoding.UTF8.GetBytes(password + salt); //SALT 적용
            var hash = sha.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }
    }
}