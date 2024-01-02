using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Utilities.Encoders;

namespace Comet.Shared
{
    public static class ConquerAccount
    {
        /// <summary>
        ///     Validates the user's inputted password, which has been decrypted from the client
        ///     request decode method, and is ready to be hashed and compared with the SHA-1
        ///     hash in the database.
        /// </summary>
        /// <param name="input">Inputted password from the client's request</param>
        /// <param name="hash">Hashed password in the database</param>
        /// <param name="salt">Salt for the hashed password in the database.</param>
        /// <returns>Returns true if the password is correct.</returns>
        public static bool CheckPassword(string input, string hash, string salt)
        {
            return HashPassword(input, salt).Equals(hash);
        }

        public static string HashPassword(string password, string salt)
        {
            byte[] inputHashed;
            using (var sha256 = SHA256.Create())
            {
                inputHashed = sha256.ComputeHash(Encoding.ASCII.GetBytes(password + salt));
            }

            string final = Hex.ToHexString(inputHashed);
            return final;
        }

        public static string GenerateSalt()
        {
            const string upperS = "QWERTYUIOPASDFGHJKLZXCVBNM";
            const string lowerS = "qwertyuioplkjhgfdsazxcvbnm";
            const string numberS = "1236547890";
            const string poolS = upperS + lowerS + numberS;
            const int sizeI = 30;

            var output = new StringBuilder();
            for (var i = 0; i < sizeI; i++)
                output.Append(poolS[RandomNumberGenerator.GetInt32(int.MaxValue) % poolS.Length]);
            return output.ToString();
        }
    }
}
