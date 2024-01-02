using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Comet.Network.Security
{
    public sealed class AesCipher : ICipher
    {
        public static byte[] SharedKey => Encoding.ASCII.GetBytes("8y/B?E(H+MbQeThWmZq3t6w9z$C&F)J@");
        public static byte[] SharedEncryptIv => Encoding.ASCII.GetBytes("t6w9z$C&F)J@NcRf");
        public static byte[] SharedDecryptIv => Encoding.ASCII.GetBytes("t6w9z$C&F)J@NcRf");

        private Aes mAes;
        private ICryptoTransform mEncryptor;
        private ICryptoTransform mDecryptor;

        private byte[] mEncryptIv;
        private byte[] mDecryptIv;

        private AesCipher()
        {
        }

        public static AesCipher Create()
        {
            var cipher = new AesCipher
            {
                mAes = Aes.Create()
            };

            cipher.mAes.Mode = CipherMode.CFB;
            cipher.mAes.KeySize = 256;
            cipher.mAes.BlockSize = 128;
            cipher.mAes.FeedbackSize = 8;
            cipher.mAes.Padding = PaddingMode.None;

            byte[] key = new byte[SharedKey.Length];
            byte[] eIv = new byte[SharedEncryptIv.Length];
            byte[] dIv = new byte[SharedDecryptIv.Length];

            SharedKey.CopyTo(key, 0);
            SharedEncryptIv.CopyTo(eIv, 0);
            SharedDecryptIv.CopyTo(dIv, 0);

            cipher.mAes.Key = key;
            cipher.mEncryptIv = eIv;
            cipher.mDecryptIv = dIv;

            cipher.mEncryptor = cipher.mAes.CreateEncryptor(cipher.mAes.Key, cipher.mEncryptIv);
            cipher.mDecryptor = cipher.mAes.CreateDecryptor(cipher.mAes.Key, cipher.mDecryptIv);
            return cipher;
        }

        /// <inheritdoc />
        public void GenerateKeys(object[] seeds)
        {
            byte[] key = (byte[])seeds[0];
            key.CopyTo(new Memory<byte>(mAes.Key));

            if (seeds.Length > 2)
            {
                byte[] eIv = (byte[])seeds[1];
                byte[] dIv = (byte[])seeds[2];
                eIv.CopyTo(new Memory<byte>(mEncryptIv));
                dIv.CopyTo(new Memory<byte>(mDecryptIv));
            }
            else
            {
                mEncryptIv = new byte[SharedEncryptIv.Length];
                mDecryptIv = new byte[SharedDecryptIv.Length];

                SharedEncryptIv.CopyTo(mEncryptIv, 0);
                SharedDecryptIv.CopyTo(mDecryptIv, 0);
            }

            mEncryptor = mAes.CreateEncryptor(mAes.Key, mEncryptIv);
            mDecryptor = mAes.CreateDecryptor(mAes.Key, mDecryptIv);
        }

        /// <inheritdoc />
        public void Decrypt(Span<byte> src, Span<byte> dst)
        {
            using MemoryStream memory = new();
            using CryptoStream stream = new(memory, mDecryptor, CryptoStreamMode.Write);
            stream.Write(src);
            stream.FlushFinalBlock();

            byte[] decrypted = memory.ToArray();
            for (int i = 0; i < decrypted.Length; i++)
            {
                dst[i] = decrypted[i];
            }
        }

        /// <inheritdoc />
        public void Encrypt(Span<byte> src, Span<byte> dst)
        {
            using var memory = new MemoryStream();
            using var stream = new CryptoStream(memory, mEncryptor, CryptoStreamMode.Write);
            stream.Write(src);
            stream.FlushFinalBlock();

            byte[] encrypted = memory.ToArray();
            for (int i = 0; i < encrypted.Length; i++)
            {
                dst[i] = encrypted[i];
            }
        }
    }
}