using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpuem.InstructionSet
{
    public class MessageInstruction : Instruction
    {
        public readonly new int length;
        bool hasimm = false;
        Hash imm = null;
        int argsize = 0;

        internal MessageInstruction(byte[] rawcode, int offset = 0)
        {
            length = 2;
            ushort inst = BitConverter.ToUInt16(rawcode, offset);
            inst >>= 3; // skip type and rsvd bits
            if ((inst & 1) != 0)
                throw new ReservedInstructionException(
                    "set message bit is reserved");
            inst >>= 1; // skip msg bit
            hasimm = (inst & 1) == 1;
            inst >>= 1; // skip imm bit
            argsize = inst;
            if (hasimm)
            {
                imm = new Hash(
                    rawcode
                    .Skip(2)
                    .Take(Hash.HASH_SIZE)
                    .ToArray());
                length += Hash.HASH_SIZE;
            }
        }

        public override byte[] get_bytes()
        {
            byte[] bytes = new byte[length];
            bytes[0] |= (byte)
                (1
                | (hasimm ? 4 : 0)
                | ((argsize >> 8) << 5));
            bytes[1] = (byte)(argsize & 0xff);
            if (hasimm)
            {
                if (imm == null)
                    throw new InvalidOperationException(
                        "has imm hash but hash was null");
                Array.Copy(imm.value, 0, bytes, 2, Hash.HASH_SIZE);
            }
            return bytes;
        }
    }
}
