using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpuem.InstructionSet
{
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
                (6 // op instruction prefix
                | (hasimm ? 8 : 0)
                | (rawdatatype << 4));
            bytes[1] |= (byte)
                (rawopmap
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

        static byte[] get_numeric_bytes(object value)
        {
            if (!value.GetType().IsValueType)
                throw new ArgumentException(
                    "value is not value type");

            Type type = value.GetType();

            if (type == typeof(byte)
                || type == typeof(ushort))
                return BitConverter.GetBytes(
                    (ushort)value);
            else if (type == typeof(uint))
                return BitConverter.GetBytes(
                    (uint)value);
            else if (type == typeof(ulong))
                return BitConverter.GetBytes(
                    (ulong)value);
            else if (type == typeof(sbyte)
                || type == typeof(short))
                return BitConverter.GetBytes(
                    (short)value);
            else if (type == typeof(int))
                return BitConverter.GetBytes(
                    (int)value);
            else if (type == typeof(long))
                return BitConverter.GetBytes(
                    (long)value);
            else if (type == typeof(float))
                return BitConverter.GetBytes(
                    (float)value);
            else if (type == typeof(double))
                return BitConverter.GetBytes(
                    (double)value);
            throw new ArgumentException(
                "value is an unknown type");
        }

        OperationInstruction(
            Type datatype,
            int rawopmap,
            int rawop)
        {
            this.datatype = datatype;
            this.rawdatatype = get_datatype(datatype);
            this.opmap = opmaptable[rawopmap];
            this.rawop = rawop;
            this.token = opmap[rawop];
            this.length = 2;
        }

        static byte[] get_list_bytes(IList list, Type type)
        {
            if (!type.IsValueType
                || type != typeof(IList)
                || type != typeof(Function)
                || type != typeof(Node))
                throw new ArgumentException(
                    "type is not a machine type");

            uint header = (uint)(list.Count);
            header <<= 4;
            header |= (uint)get_datatype(type);
            byte[] bytes =
                BitConverter.GetBytes(
                    header);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(bytes, 0, bytes.Length);
                foreach (object o in list)
                {
                    if (type.IsValueType)
                        bytes = get_numeric_bytes(o);
                    else if (type == typeof(Function))
                        bytes = ((Function)o).hash.value;
                    else if (type == typeof(Node))
                        bytes = ((Node)o).hash.value;
                    else if (type == typeof(IList))
                        bytes = get_list_bytes(
                            list,
                            list.GetType()
                                .GenericTypeArguments[0]);
                    ms.Write(bytes, 0, bytes.Length);
                }
                return ms.ToArray();
            }
        }

        public OperationInstruction(
            int rawopmap,
            int rawop,
            IList list,
            Type listtype)
            : this(typeof(IList), rawopmap, rawop)
        {
            this.imm = list;
            hasimm = imm != null;
            if (!hasimm)
                throw new ArgumentException(
                    "list was null");
            rawimm = get_list_bytes(list, listtype);
        }

        public OperationInstruction(
            Type datatype,
            int rawopmap,
            int rawop,
            object imm = null)
            : this(datatype, rawopmap, rawop)
        {
            this.imm = imm;
            hasimm = imm != null;
            if (!hasimm)
                return;

            // serialize imm
            if (datatype.IsValueType)
                rawimm = get_numeric_bytes(imm);
            else if (datatype == typeof(Function))
                rawimm = ((Function)imm).hash.value;
            else if (datatype == typeof(Node))
                rawimm = ((Node)imm).hash.value;

            length += rawimm.Length;

            throw new NotImplementedException();
        }

        static object convert_value_imm(Type type, byte[] rawvalue, int offset)
        {
            if (!type.IsValueType)
                throw new ArgumentException(
                    "type is not a value type");
            var imm = type.Equals(typeof(sbyte))
                    ? (sbyte)rawvalue[offset + 1]
                : type.Equals(typeof(byte))
                    ? rawvalue[offset + 1]
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
}
