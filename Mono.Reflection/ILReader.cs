using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Reflection {

    public class ILReader {
        private BinaryReader _reader;
        private ITokenResolver _resolver;
        private bool _sentinelHit = false;

        public ILReader(
            BinaryReader reader, 
            ITokenResolver resolver) 
        {
            _reader = reader;
            _resolver = resolver;
        }

        enum ElementType {
            End = 0x00,

            Void = 0x01,
            Boolean = 0x02,
            Char = 0x03,
            SByte = 0x04,
            Byte = 0x05,
            Int16 = 0x06,
            UInt16 = 0x07,
            Int32 = 0x08,
            UInt32 = 0x09,
            Int64 = 0x0a,
            UInt64 = 0x0b,
            Single = 0x0c,
            Double = 0x0d,
            String = 0x0e,

            // followed by <type> token
            Pointer = 0x0f,
            Reference = 0x10,
            ValueType = 0x11,
            Class = 0x12,

            //followed by <type><rank><boundsCount><bound1>...<loCount><lo1>...
            Array = 0x14,
            TypedReference = 0x16,
            IntPtr = 0x18,
            UIntPtr = 0x19,

            // followed by full method signature
            FunctionPointer = 0x1b,

            Object = 0x1c,

            SZArray = 0x1d,
            Required = 0x1f,
            Optional = 0x20,
            Internal = 0x21,

            Modifier = 0x40,
            Sentinel = 0x41,
            Pinned = 0x45,
        }

        enum TokenType {
            TypeDef = 0b00,
            TypeRef = 0b01,
            TypeSpec = 0b10,
            Invalid = 0b11
        }

        public byte ReadByte() {
            return _reader.ReadByte();
        }

        public int ReadSerializedInt32()
        {
            byte firstByte = _reader.ReadByte();
            // highest bit 0 -> single-byte value
            if (((firstByte >> 7) & 0x1) == 0x0)
            {
                return firstByte & 0x7f;
            }
            // two-byte value
            else if (((firstByte >> 6) & 0b11) == 0b10)
            {
                byte secondByte = _reader.ReadByte();
                return ((firstByte & 0x3f) << 8) | secondByte;
            }
            // four-byte value
            else if (((firstByte >> 6) & 0b11) == 0b11)
            {
                byte[] nextThree = _reader.ReadBytes(3);
                return ((firstByte & 0x3f) << 24)
                    | (nextThree[0] << 16)
                    | (nextThree[1] << 8)
                    | nextThree[2];
            }
            throw new InvalidDataException("Could not read an IL serialized field from the stream");
        }

        public Type ReadTypeSignature(out VariableFlags flags) {
            // initialize flags
            flags = _sentinelHit ? VariableFlags.Optional : 0;

            ElementType typeCode = (ElementType)_reader.ReadByte();

            // check for modifiers
            switch(typeCode) {
                case ElementType.Sentinel:
                    _sentinelHit = true;
                    flags |= VariableFlags.Optional;
                    typeCode = (ElementType)_reader.ReadByte();
                    break;
                case ElementType.Pinned:
                    flags |= VariableFlags.Pinned;
                    typeCode = (ElementType)_reader.ReadByte();
                    break;
            }

            switch (typeCode)
            {
                case ElementType.Sentinel:
                    _sentinelHit = true;
                    return ReadTypeSignature(out flags);
                case ElementType.Void: return typeof(void);
                case ElementType.Boolean: return typeof(bool);
                case ElementType.Char: return typeof(char);
                case ElementType.SByte: return typeof(sbyte);
                case ElementType.Byte: return typeof(byte);
                case ElementType.Int16: return typeof(short);
                case ElementType.UInt16: return typeof(ushort);
                case ElementType.Int32: return typeof(int);
                case ElementType.UInt32: return typeof(uint);
                case ElementType.Int64: return typeof(long);
                case ElementType.UInt64: return typeof(ulong);
                case ElementType.Single: return typeof(float);
                case ElementType.Double: return typeof(double);
                case ElementType.String: return typeof(string);
                case ElementType.IntPtr: return typeof(IntPtr);
                case ElementType.UIntPtr: return typeof(UIntPtr);
                case ElementType.TypedReference:
                case ElementType.Object:
                    var token = ReadSerializedInt32();
                    var tokenType = (TokenType)(token & 0b11);

                    switch (tokenType)
                    {
                        case TokenType.TypeDef:
                        case TokenType.TypeRef:
                        case TokenType.TypeSpec:
                            return _resolver.ResolveType(token >> 2, new Type[0], new Type[0]);
                        case TokenType.Invalid:
                        default:
                            throw new InvalidDataException("Invalid token type");
                    }
                default:
                    throw new InvalidDataException("Invalid type code");
            }
        }
    }
}
