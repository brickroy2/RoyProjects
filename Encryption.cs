using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Platformer2
{
    public class Encryption
    {
        public static string CastRSAKeyToString(RSAParameters key) => JsonConvert.SerializeObject(key);
        // Makes an RSAParameters type into a string to send in the net

        public static RSAParameters CastStringToRSAKey(string keyString) => JsonConvert.DeserializeObject<RSAParameters>(keyString);
        // Cast a string into an RSAParameters key

        public static string EncryptString(string plainText, byte[] key)
        { // AES symmetric encryption of string
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = new byte[16];

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        public static string DecryptString(string cipherText, byte[] key)
        { // AES symmetric decryption of string
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = new byte[16];

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
        public static byte[] GenerateSymmetricKey()
        { //generate a random symmetric AES key (used by client only)
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                return aes.Key;
            }
        }
        public static string Hash(string message)
        {
            // Create a SHA256 hash to a string
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(message));
                string hashed = Convert.ToBase64String(bytes);
                return hashed.Replace('+', '-').Replace('/', '_').Trim('=');
            }
        }
    }
}
