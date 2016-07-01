using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cpuem.InstructionSet;

namespace cpuem
{
    class Function
    {
        const int MAX_CODE_LENGTH = 0x1000;
        public readonly Hash hash;
        readonly List<Instruction> instructions;

        public Function(byte[] rawcode)
        {
            if (rawcode.Length > MAX_CODE_LENGTH)
                throw new InvalidOperationException(string.Format(
                    "code length must be less than or equal to {0}.",          
                    MAX_CODE_LENGTH));

            hash = new Hash(rawcode, rawcode.Length);
            instructions = new List<Instruction>();
            for (int offset = 0; offset < rawcode.Length; /**/)
            {
                instructions.Add(
                    Instruction.decode(rawcode, offset));
                offset += instructions.Last().length;
            }
        }
    }
}
