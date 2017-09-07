using System;
using System.Collections.Generic;
using System.Linq;
using UnityStudio.Extensions;

namespace UnityStudio.Models {
    public sealed class AssetPreloadData {

        internal AssetPreloadData(AssetFile source) {
            Source = source;
        }

        public long PathID { get; internal set; }

        public uint Offset { get; internal set; }

        public int Size { get; internal set; }

        public string TypeName { get; internal set; }

        public int Type1 { get; internal set; }

        public ushort Type2 { get; internal set; }

        public string UniqueID { get; internal set; }

        public AssetFile Source { get; }

        public bool CheckIsKnownType() {
            var t = Type2;
            var knownTypes = (int[])Enum.GetValues(typeof(KnownClassID));
            return knownTypes.Any(t1 => t1 == t);
        }

        public KnownClassID KnownType => (KnownClassID)Type2;

        internal IReadOnlyDictionary<string, object> GetStructure() {
            var source = Source;

            if (!source.Objects.ContainsKey(Type1)) {
                return null;
            }

            source.FileReader.Position = Offset;
            var members = source.Objects[Type1];
            return Read(source.FileReader, members.Children);
        }

        private static IReadOnlyDictionary<string, object> Read(EndianBinaryReader reader, IReadOnlyList<ClassMember> classMembers) {
            return Read(reader, classMembers, 0, 0, false);
        }

        private static IReadOnlyDictionary<string, object> Read(EndianBinaryReader reader, IReadOnlyList<ClassMember> classMembers, int startIndex) {
            return Read(reader, classMembers, startIndex, 0, false);
        }

        private static IReadOnlyDictionary<string, object> Read(EndianBinaryReader reader, IReadOnlyList<ClassMember> classMembers, int startIndex, int baseLevel, bool fastReturn) {
            var dict = new Dictionary<string, object>();

            for (var i = startIndex; i < classMembers.Count; ++i) {
                var member = classMembers[i];
                var level = member.Level;

                if (level <= baseLevel && fastReturn) {
                    return dict;
                }

                var varName = member.Name;
                var varType = member.TypeName;
                var shouldAlign = (member.Flags & 0x4000) != 0;

                object value;
                IReadOnlyList<ClassMember> subList;
                switch (varType) {
                    case "SInt8":
                        value = reader.ReadSByte();
                        break;
                    case "UInt8":
                        value = reader.ReadByte();
                        break;
                    case "SInt16":
                    case "short":
                        value = reader.ReadInt16();
                        break;
                    case "UInt16":
                    case "unsigned short":
                        value = reader.ReadUInt16();
                        break;
                    case "SInt32":
                    case "int":
                        value = reader.ReadInt32();
                        break;
                    case "UInt32":
                    case "unsigned int":
                        value = reader.ReadUInt32();
                        break;
                    case "SInt64":
                    case "long long":
                        value = reader.ReadInt64();
                        break;
                    case "UInt64":
                    case "unsigned long long":
                        value = reader.ReadUInt64();
                        break;
                    case "float":
                        value = reader.ReadSingle();
                        break;
                    case "double":
                        value = reader.ReadDouble();
                        break;
                    case "bool":
                        value = reader.ReadBoolean();
                        break;
                    case "string":
                        var strLen = reader.ReadInt32();
                        value = reader.ReadAlignedString(strLen);
                        i += 3;
                        break;
                    case "Array":
                        if (i > 0 && (classMembers[i - 1].Flags & 0x4000) != 0) {
                            shouldAlign = true;
                        }

                        var size = reader.ReadInt32();
                        subList = ReadSubObjectList(classMembers, level, i + 2);

                        var list = new List<object>();
                        for (var j = 0; j < size; ++j) {
                            var d2 = Read(reader, subList);
                            list.Add(d2);
                        }

                        // '1' for "size" field.
                        i += 1 + subList.Count;

                        value = list;
                        break;
                    default:
                        var customType = new CustomType {
                            TypeName = varType,
                            Name = varName,
                            Level = level
                        };

                        subList = ReadSubObjectList(classMembers, level, i + 1);
                        var objects = Read(reader, subList);
                        customType.Variables = objects;
                        value = customType;

                        i += subList.Count;

                        shouldAlign = false;
                        break;
                }

                dict[varName] = value;

                if (shouldAlign) {
                    reader.AlignBy(4);
                }
            }

            return dict;
        }

        private static IReadOnlyList<ClassMember> ReadSubObjectList(IReadOnlyList<ClassMember> classMembers, int thisLevel, int startIndex) {
            var cl2 = new List<ClassMember>();
            for (var i = startIndex; i < classMembers.Count; ++i) {
                if (classMembers[i].Level <= thisLevel) {
                    return cl2;
                }
                cl2.Add(classMembers[i]);
            }
            return cl2;
        }

    }
}
