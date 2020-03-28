﻿using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption.Stream
{

    public class StreamAesBouncyCastleEncryptor : StreamEncryptor
    {
        byte[] cfbBuf = new byte[MaxInputSize + 128];
        int ptr = 0;
        //ExtendedCfbBufferedBlockCipher c;
        ExtendedCfbBlockCipher b;
        public StreamAesBouncyCastleEncryptor(string method, string password) : base(method, password)
        {
            b = new ExtendedCfbBlockCipher(new AesEngine(), 128);
            // c = new ExtendedCfbBufferedBlockCipher(b);
        }

        protected override void initCipher(byte[] iv, bool isEncrypt)
        {
            base.initCipher(iv, isEncrypt);
            b.Init(isEncrypt, new ParametersWithIV(new KeyParameter(key), iv));
        }

        protected override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            CipherUpdate(plain, cipher);
            return plain.Length;
        }

        protected override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            CipherUpdate(cipher, plain);
            return cipher.Length;
        }

        private void CipherUpdate(Span<byte> i, Span<byte> o)
        {
            Span<byte> ob = new byte[o.Length + 128];
            i.CopyTo(cfbBuf.AsSpan(ptr));
            // TODO: standard CFB
            int total = i.Length + ptr;

            int blkSize = b.GetBlockSize();

            int blkCount = total / blkSize;
            int restSize = total % blkSize;
            int readPtr = 0;

            byte[] tmp = new byte[blkSize];
            for (int j = 0; j < blkCount; j++)
            {
                b.ProcessBlock(cfbBuf, readPtr, tmp, 0);
                tmp.CopyTo(ob.Slice(readPtr));
                readPtr += blkSize;
            }
            if (readPtr != blkSize * blkCount) throw new System.Exception();
            b.ProcessBlock(cfbBuf, readPtr, tmp, 0, false);
            tmp.CopyTo(ob.Slice(readPtr));
            Array.Copy(cfbBuf, readPtr, cfbBuf, 0, restSize);
            ob.Slice(ptr, o.Length).CopyTo(o);
            ptr = restSize;


        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"aes-128-cfb",new CipherInfo("aes-128-cfb", 16, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
            {"aes-192-cfb",new CipherInfo("aes-192-cfb", 24, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
            {"aes-256-cfb",new CipherInfo("aes-256-cfb", 32, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
        };

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }

        protected override Dictionary<string, CipherInfo> getCiphers()
        {
            return _ciphers;
        }
        #endregion
    }
}