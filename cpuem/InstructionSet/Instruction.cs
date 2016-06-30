using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace cpuem.InstructionSet
{
    public class InvalidInstructionException : Exception
    {
        public InvalidInstructionException(string message) 
            : base(message)
        {
        }
    }
    public class ReservedInstructionException : Exception
    {
        public ReservedInstructionException(string message) : base(message)
        {
        }
    }

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

    public class OperationInstruction : Instruction
    {
        public readonly new int length;
        public readonly bool hasimm;
        public readonly Type datatype;
        public readonly OpMap opmap;
        public readonly string token;
        public readonly object imm;

        readonly int rawdatatype;
        readonly int rawopmap;
        readonly int rawop;
        readonly byte[] rawimm;

        static int get_datatype_numeric_size(Type type)
        {
            if (!type.IsValueType)
                goto get_datatype_not_numeric;
            if (type == typeof(byte)
                || type == typeof(sbyte))
                return 0;
            if (type == typeof(ushort)
                || type == typeof(short)
                || type == typeof(float))
                return 1;
            if (type == typeof(uint)
                || type == typeof(int)
                || type == typeof(double))
                return 2;
            if (type == typeof(ulong)
                || type == typeof(long))
                return 3;
            get_datatype_not_numeric:
            throw new ArgumentException(
                "type was not numeric");
        }

        static int get_datatype(Type type)
        {
            int rawtype = 0;
            if (type.IsValueType)
            {
                rawtype = get_datatype_numeric_size(type);
                if (type == typeof(sbyte)
                    || type == typeof(short)
                    || type == typeof(int)
                    || type == typeof(long))
                    rawtype |= 2;
                else if (type == typeof(float)
                    || type == typeof(double))
                    rawtype |= 1;
                return rawtype;
            }
            throw new NotImplementedException(
                "non-value types not implemented");
        }

        static Type get_datatype(ushort inst, out int length)
        {
            inst >>= 4; // skip stuff ...
            switch (inst & 8) // get data type
            {
                case 0:   // is integer
                    switch (inst & 4) // get signededness
                    {
                        case 0: // unsigned
                            switch (inst & 3)   // get size
                            {
                                case 0:
                                    length = sizeof(byte);
                                    return typeof(byte);
                                case 1:
                                    length = sizeof(ushort);
                                    return typeof(ushort);
                                case 2:
                                    length = sizeof(uint);
                                    return typeof(uint);
                                case 3:
                                    length = sizeof(ulong);
                                    return typeof(ulong);
                            }
                            break;
                        case 4: // signed
                            switch (inst & 3)   // get size
                            {
                                case 0:
                                    length = sizeof(sbyte);
                                    return typeof(sbyte);
                                case 1:
                                    length = sizeof(short);
                                    return typeof(short);
                                case 2:
                                    length = sizeof(int);
                                    return typeof(int);
                                case 3:
                                    length = sizeof(long);
                                    return typeof(long);
                            }
                            break;
                    }
                    break;
                case 8: // is other
                    switch (inst & 4)
                    {
                        case 0: // is float
                            switch (inst & 3) // get size
                            {
                                case 0: // is half, but unsupported
                                case 3: // is quad, but unsupported
                                    throw new ReservedInstructionException(
                                        "reserved float type");
                                case 1: // is single
                                    length = sizeof(float);
                                    return typeof(float);
                                case 2: // is double
                                    length = sizeof(double);
                                    return typeof(double);
                            }
                            break;
                        case 4: // is reference
                            switch (inst & 3)
                            {
                                case 0:
                                    length = Hash.HASH_SIZE;
                                    return typeof(Function);
                                case 1:
                                    length = Hash.HASH_SIZE;
                                    return typeof(Node);
                                case 2:
                                    throw new NotImplementedException(
                                        "list datatype");
                                case 3:
                                    throw new ReservedInstructionException(
                                        "reserved datatype");
                            }
                            break;
                    }
                    break;
            }
            throw new InvalidOperationException(
                "some bad stuff happened");
        }

        public override byte[] get_bytes()
        {
            byte[] bytes = new byte[length];
            bytes[0] |= (byte)
                ( 6 // op instruction prefix
                | (hasimm ? 8 : 0)
                | (rawdatatype << 4));
            bytes[1] |= (byte)
                ( rawopmap
                | rawop);
            if (hasimm)
            {
                if (rawimm == null)
                    throw new InvalidOperationException(
                        "has imm but raw imm was null");
                Array.Copy(rawimm, 0, bytes, 2, rawimm.Length);
            }
            return bytes;
        }

        static int get_datatype_size(Type type)
        {
            if (type.IsValueType)
                return get_datatype_numeric_size(type);
            if (type == typeof(Function)
                || type == typeof(Node))
                return Hash.HASH_SIZE;
            throw new ArgumentException(
                "couldn't get datatype size.\n"
                + "if type is list, use \"get_list_size()\".");
        }

        static int get_list_size(byte[] rawcode, int listoffset)
        {
            int size = -1;
            Type entrytype =
                get_datatype(
                    (ushort)(rawcode[listoffset] << 4),
                    out size);
            size *= (int)(BitConverter.ToUInt32(rawcode, listoffset) >> 4);
            if (entrytype != typeof(IList))
                return size;
            int entrycount = size;
            for (int i = 0; i < entrycount; i++)
            {
                listoffset += 4;
                size += get_list_size(rawcode, listoffset);
            }
            return size;
        }

        public OperationInstruction(
            Type datatype, 
            OpMap opmap, 
            int rawop, 
            object imm = null)
        {
            throw new NotImplementedException();   
        }

        static object convert_value_imm(Type type, byte[] rawvalue, int offset)
        {
            if (!type.IsValueType)
                throw new ArgumentException(
                    "type is not a value type");
            var imm = type.Equals(typeof(sbyte))
                    ? (sbyte)rawvalue[offset]
                : type.Equals(typeof(byte))
                    ? rawvalue[offset]
                : type.Equals(typeof(ushort))
                    ? BitConverter.ToUInt16(rawvalue, offset)
                : type.Equals(typeof(short))
                    ? BitConverter.ToInt16(rawvalue, offset)
                : type.Equals(typeof(uint))
                    ? BitConverter.ToUInt32(rawvalue, offset)
                : type.Equals(typeof(int))
                    ? BitConverter.ToInt32(rawvalue, offset)
                : type.Equals(typeof(ulong))
                    ? BitConverter.ToUInt64(rawvalue, offset)
                : type.Equals(typeof(long))
                    ? BitConverter.ToInt64(rawvalue, offset)
                : type.Equals(typeof(float))
                    ? BitConverter.ToSingle(rawvalue, offset)
                : type.Equals(typeof(double))
                    ? BitConverter.ToDouble(rawvalue, offset)
                : Type.Missing;
            if (imm == Type.Missing)
                throw new InvalidInstructionException(
                    "imm without a type");
            return imm;
        }

        internal OperationInstruction(OpMap[] opmaptable, byte[] rawcode, int offset = 0)
        {
            int typelength;
            if (opmaptable == null || opmaptable.Length < 1)
                throw new ArgumentNullException(
                    "null or empty opmap table");
            ushort inst = BitConverter.ToUInt16(rawcode, offset);
            inst >>= 1; // skip type bit
            if ((inst & 3) != 3)
                throw new InvalidOperationException(
                    "operation flags not set (gt | lt in branch)");
            inst >>= 2; // skip op flags
            hasimm = (inst & 1) == 1;
            inst >>= 1; // skip hasimm
            rawdatatype = inst & 0xf;
            inst <<= 4; // for get_datatype() call
            datatype = get_datatype(inst, out typelength);
            inst >>= 8; // skip first byte
            rawopmap = inst & 0xf;
            opmap = opmaptable[rawopmap];
            inst >>= 4; // skip opmap
            rawop = inst & 0xf;
            token = opmap[rawop];
            if (hasimm)
            {
                int listlen = -1;
                offset += 2;
                rawimm = rawcode.Skip(offset).ToArray();
                throw new NotImplementedException(
                    "function and node types need cache lookup");
                imm =
                    datatype.IsValueType
                        ? convert_value_imm(datatype, rawimm, 0)
                    : datatype.Equals(typeof(Function))
                        ? rawcode.Skip(offset).Take(Hash.HASH_SIZE)
                    : datatype.Equals(typeof(Node))
                        ? rawcode.Skip(offset).Take(Hash.HASH_SIZE)
                    : datatype.Equals(typeof(IList))
                        ? build_list(rawimm)
                    : Type.Missing;
                if (imm == Type.Missing)
                    throw new InvalidInstructionException(
                        "imm without a type");
                length += typelength;
            }
            length += 2;
        }

        private IList build_list(byte[] rawlist)
        {
            int typesize = -1;
            Type listtype =
                typeof(List<>).MakeGenericType(
                    get_datatype(
                        (ushort)(rawlist[0] << 4),
                        out typesize));
            IList list = (IList)
                Activator.CreateInstance(listtype);
            int entrycount = (int)(BitConverter.ToUInt32(rawlist, 0) >> 4);
            if (listtype.IsValueType)
            {
                for (int i = 0; i < entrycount; i++)
                    list.Add(
                        convert_value_imm(
                            listtype, 
                            rawlist, 
                            i * typesize));
                return list;                
            }
            throw new NotImplementedException(
                "need function and node lookups");
        }
    }

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
                ( 1
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

    public abstract class Instruction
    {
        const int OPMAP_TABLE_SIZE = 16;
        const int MIN_INST_LEN = 2;
        public readonly int length = -1;

        static readonly OpMap[] opmaptable = new OpMap[OPMAP_TABLE_SIZE];

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
