using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace cpuem.InstructionSet
{








    public abstract class Instruction
    {
        const int OPMAP_TABLE_SIZE = 16;
        const int MIN_INST_LEN = 2;
        public readonly int length = -1;

        protected static readonly OpMap[] opmaptable = new OpMap[OPMAP_TABLE_SIZE];

        public abstract byte[] get_bytes();

        public static void set_opmap_table_entry(OpMap opmap, int slot)
        {
            if (slot < 0 || slot >= opmaptable.Length)
                throw new IndexOutOfRangeException(
                    "invalid opmap table slot");
            opmaptable[slot] = opmap;
        }

        public static OpMap get_opmap_table_entry(int slot)
        {
            if (slot < 0 || slot >= opmaptable.Length)
                throw new IndexOutOfRangeException(
                    "invalid opmap table slot");
            return opmaptable[slot];
        }

        public static Instruction decode(byte[] rawcode, int offset = 0)
        {
            if (rawcode.Length - offset < MIN_INST_LEN)
                throw new InvalidInstructionException(
                    "instruction was an invalid size");
            
            switch (rawcode[0] & 1)     // bit 0
            {
                case 0:                 // exec type instruction
                    switch ((rawcode[0] >> 1) & 3)
                    {
                        case 3:         // op type instruction
                            return new OperationInstruction(
                                opmaptable, rawcode, offset);
                        default:        // branch instruction
                            return new BranchInstruction(
                                rawcode, offset);
                    }
                case 1:                 // sys type instruction
                    switch ((rawcode[0] >> 1) & 0xf)
                    {
                        case 0:         // message instruction
                            return new MessageInstruction(
                                rawcode, offset);
                        default:        // unimplemented / rsvd
                            throw new ReservedInstructionException(
                                "reserved system type bits set");
                    }
            }
            throw new InvalidOperationException(
                "invalid decoding operation");
        }
    }
}
