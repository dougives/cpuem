using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpuem.InstructionSet
{
    public abstract class InstructionException : Exception
    {
        public InstructionException(string message)
            : base(message) { }
    }

    public class InvalidInstructionException 
        : InstructionException
    {
        public InvalidInstructionException(string message)
            : base(message)
        {
        }
    }
    public class ReservedInstructionException 
        : InstructionException
    {
        public ReservedInstructionException(string message) : base(message)
        {
        }
    }
}
