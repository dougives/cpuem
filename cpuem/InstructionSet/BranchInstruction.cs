using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpuem.InstructionSet
{
    public class BranchInstruction : Instruction
    {
        [Flags]
        public enum Inequality
        {
            None = 0,
            LessThan = 1,
            GreaterThan = 2,
            Invalid = 3,
        }

        public readonly new int length = 2;
        public readonly bool zero, equals;
        public readonly int offset;
        Inequality inequality;

        public BranchInstruction(
            Inequality inequality,
            bool zero,
            bool equals,
            int offset)
        {
            if (inequality == Inequality.Invalid)
                throw new ArgumentException(
                    "inequalities should be mutually exclusive");
            this.inequality = inequality;
            this.zero = zero;
            this.equals = equals; // ...
            this.offset = offset;
        }

        internal BranchInstruction(byte[] rawcode, int offset = 0)
        {
            ushort inst = BitConverter.ToUInt16(rawcode, offset);
            inst >>= 1; // skip type bit
            inequality = (Inequality)(inst & 3);
            if (inequality == Inequality.Invalid)
                throw new InvalidInstructionException(
                    "cannot have both inequalities set in branch");
            inst >>= 2; // skip inequalities
            zero = (inst & 1) == 1;
            inst >>= 1; // skip zero
            equals = (inst & 1) == 1;
            inst >>= 1;
            offset = (short)inst;

        }

        public override byte[] get_bytes()
        {
            byte[] bytes = new byte[length];
            bytes[0] |= (byte)
                (((int)inequality << 1)
                | (zero ? 8 : 0)
                | (equals ? 0x10 : 0)
                | ((offset >> 8) << 5));
            bytes[1] = (byte)(offset & 0xff);
            return bytes;
        }
    }
}
