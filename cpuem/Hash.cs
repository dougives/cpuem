using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace cpuem
{
    public class Hash
    {
        public const int HASH_SIZE = 32;
        static readonly SHA256 sha;
        public readonly byte[] value;

        static Hash()
        {
            sha = SHA256.Create();
        }

        public Hash(byte[] hash)
        {
            this.value = hash;
        }

        public Hash(byte[] data, int count)
        {
            value = sha.ComputeHash(data.Take(count).ToArray());
        }

        public Hash(Stream data)
        {
            value = sha.ComputeHash(data);
        }

        public override string ToString()
            => BitConverter.ToString(value, 0, HASH_SIZE)
                .Replace("-", "")
                .ToLower();

        public override bool Equals(object obj)
        {
            if (obj.GetHashCode() != this.GetHashCode()
                || obj.GetType() != this.GetType())
                return false;
            byte[] arg = (byte[])obj;
            if (arg.Length != HASH_SIZE)
                return false;
            for (int i = 0; i < HASH_SIZE; i++)
                if (value[i] != arg[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hashcode = 0;
            for (int i = 0; i < (HASH_SIZE >> 2); i++)
                hashcode ^= BitConverter.ToInt32(value, i << 2);
            return hashcode;
        }
    }
}
