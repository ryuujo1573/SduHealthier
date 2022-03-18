using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SduHealthier.Core.Utilities
{
    public static class Encrypting
    {
        public static string GenerateRsa(string data, string k1 = "1", string k2 = "2", string k3 = "3")
        {
            // Non-standard RSA Encryption
            var dataBytes = ExtendTo16Bits(data);
            var firstKey = ExtendTo16Bits(k1);
            var secondKey = ExtendTo16Bits(k2);
            var thirdKey = ExtendTo16Bits(k3);

            var result = new List<byte>();

            for (int i = 0; i < dataBytes.Length; i += 8)
            {
                var temp = dataBytes[i..(i + 8)];
                // var rsa = RSA.Create();
                // rsa.ImportParameters(new RSAParameters{});
                var des = SymmetricAlgorithm.Create("DES");
                if (des is null)
                {
                    throw new Exception("DES Algorithm not found.");
                }

                des.Mode = CipherMode.ECB;

                for (int j = 0; j < firstKey.Length; j += 8)
                {
                    des.Key = firstKey[j..(j + 8)];
                    temp = des.CreateEncryptor().TransformFinalBlock(temp, 0, temp.Length);
                }

                for (int j = 0; j < secondKey.Length; j += 8)
                {
                    des.Key = secondKey[j..(j + 8)];
                    temp = des.CreateEncryptor().TransformFinalBlock(temp, 0, temp.Length);
                }

                for (int j = 0; j < thirdKey.Length; j += 8)
                {
                    des.Key = thirdKey[j..(j + 8)];
                    temp = des.CreateEncryptor().TransformFinalBlock(temp, 0, temp.Length);
                }

                result.AddRange(temp);
            }

            return result.ToArray().Let(Convert.ToHexString);
        }
#if DEBUG
        public
#elif RELEASE
        private
#endif
            static byte[] ExtendTo16Bits(string input)
        {
            // todo: use only one Span.
            var bytes = new byte[input.Length];
            Encoding.ASCII.GetEncoder().GetBytes(input, bytes, false);

            int size = (int) Math.Ceiling((input.Length * 2) / 8d) * 8;
            var extended = new byte[size];
            // (int i, int j) = (0, 0);
            var i = 0;
            foreach (var b in bytes)
            {
                extended[i++] = 0;
                extended[i++] = b;
            }

            // while (i > 0)
            while (extended.Length > i)
            {
                extended[i++] = 0;
            }

            return extended;
        }
    }
}