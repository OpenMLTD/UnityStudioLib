using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityStudio.Extensions;

namespace UnityStudio.Models {
    public sealed class AssetFile : DisposableBase {

        /// <summary>
        /// Loads an asset file from a <see cref="FileStream"/>.
        /// </summary>
        /// <param name="fileStream">The <see cref="FileStream"/> which contains asset file data.</param>
        /// <param name="disposesReader">
        /// Whether disposes internally created <see cref="EndianBinaryReader"/> immediately.
        /// If the reader is disposed, accessing <see cref="FileReader"/> will throw an <see cref="ObjectDisposedException"/>.</param>
        public AssetFile(FileStream fileStream, bool disposesReader)
            : this(new EndianBinaryReader(fileStream, Endian.BigEndian), fileStream.Name, disposesReader) {
        }

        internal AssetFile(EndianBinaryReader fileReader, string fileName, bool disposesReader) {
            FileReader = fileReader;
            Initialize(fileReader, fileName);
            if (disposesReader) {
                fileReader.Dispose();
            }
        }

        public EndianBinaryReader FileReader { get; }

        public string FullFileName { get; internal set; }

        public string Version { get; internal set; }

        public string BuildType { get; internal set; }

        public AssetPlatform Platform { get; private set; }

        public IReadOnlyDictionary<int, ClassMember> Objects => _objects;

        public IReadOnlyList<SharedAssetInfo> SharedAssetList { get; private set; }

        public IReadOnlyList<AssetPreloadData> PreloadDataList { get; private set; }

        internal int FileFormatVersion { get; private set; }

        internal IReadOnlyList<int> VersionComponents { get; set; } = new[] { 0, 0, 0, 0 };

        protected override void Dispose(bool disposing) {
            FileReader.Dispose();
        }

        private void Initialize(EndianBinaryReader reader, string fileName) {
            var tableSize = reader.ReadInt32();
            var dataEnd = reader.ReadInt32();
            var fileFormatVersion = reader.ReadInt32();
            var dataOffset = reader.ReadUInt32();

            FileFormatVersion = fileFormatVersion;
            FullFileName = Path.GetFullPath(fileName);

            var baseDefinitions = false;
            var platform = 0;
            switch (fileFormatVersion) {
                case 6:
                    reader.Position = dataEnd - tableSize;
                    reader.Position += 1;
                    break;
                case 7:
                    reader.Position = dataEnd - tableSize;
                    reader.Position += 1;
                    Version = reader.ReadStringToNull();
                    break;
                case 8:
                    reader.Position = dataEnd - tableSize;
                    reader.Position += 1;
                    Version = reader.ReadStringToNull();
                    platform = reader.ReadInt32();
                    break;
                case 9:
                    reader.Position += 4;
                    Version = reader.ReadStringToNull();
                    platform = reader.ReadInt32();
                    break;
                case 14:
                case 15:
                case 16:
                case 17:
                    reader.Position += 4;
                    Version = reader.ReadStringToNull();
                    platform = reader.ReadInt32();
                    baseDefinitions = reader.ReadBoolean();
                    break;
                default:
                    throw new FormatException();
            }

            if (platform > 255 || platform < 0) {
                platform = EndianHelper.SwapEndian(platform);
                reader.Endian = Endian.LittleEndian;
            }
            Platform = (AssetPlatform)platform;

            ReadClasses(reader, fileFormatVersion, baseDefinitions);

            if (7 <= fileFormatVersion && fileFormatVersion < 14) {
                reader.Position += 4;
            }

            ReadAssetsPreloadTable(reader, fileFormatVersion, dataOffset);

            // May be buggy...
            // The original author assumes these two properties are not used in Unity 2.5.x (in BuildSettings class).
            VersionComponents = VersionSplitter
                .Split(Version)
                .Select(str => Convert.ToInt32(str))
                .ToArray();
            BuildType = BuildTypePattern.Match(Version).Groups["build"].Value;

            // 2017.x version handling is solved by better pattern splitting, so we skipped here.

            if (fileFormatVersion >= 14) {
                var someCount = reader.ReadInt32();
                for (var i = 0; i < someCount; ++i) {
                    var dummy1 = reader.ReadInt32();
                    reader.AlignBy(4);
                    var pathID = reader.ReadInt64();
                }
            }

            var sharedFileCount = reader.ReadInt32();
            var sharedFileList = new SharedAssetInfo[sharedFileCount];
            for (var i = 0; i < sharedFileCount; ++i) {
                var shared = new SharedAssetInfo();
                shared.SomeName = reader.ReadStringToNull();
                reader.Position += 20;
                var sharedFileName = reader.ReadStringToNull();
                shared.FileName = sharedFileName;
                sharedFileList[i] = shared;
            }
            SharedAssetList = sharedFileList;
        }

        private void ReadClasses(EndianBinaryReader reader, int fileFormatVersion, bool baseDefinitions) {
            var baseCount = reader.ReadInt32();
            for (var i = 0; i < baseCount; ++i) {
                if (fileFormatVersion < 14) {
                    var classID = reader.ReadInt32();
                    var baseType = reader.ReadStringToNull();
                    var baseName = reader.ReadStringToNull();
                    reader.Position += 20;

                    var memberCount = reader.ReadInt32();
                    var members = new List<ClassMember>();
                    for (var j = 0; j < memberCount; ++j) {
                        var classMember = ReadClassMember(reader, 0, baseType, baseName);
                        members.Add(classMember);
                        _objects[classID] = classMember;
                    }
                } else {
                    ReadClassMember5(reader, fileFormatVersion, baseDefinitions);
                }
            }
        }

        private static ClassMember ReadClassMember(EndianBinaryReader reader, int level, string baseType, string baseName) {
            var varType = reader.ReadStringToNull();
            var varName = reader.ReadStringToNull();
            var size = reader.ReadInt32();
            var index = reader.ReadInt32();
            var isArray = reader.ReadInt32() != 0;
            var dummy = reader.ReadInt32();
            var flags = reader.ReadInt32();
            var childrenCount = reader.ReadInt32();

            var children = new List<ClassMember>();

            for (var i = 0; i < childrenCount; ++i) {
                var member = ReadClassMember(reader, level + 1, varType, varName);
                children.Add(member);
            }

            var classMember = new ClassMember(level, baseType, baseName, varType, varName, size, flags, children);

            return classMember;
        }

        private void ReadClassMember5(EndianBinaryReader reader, int fileFormatVersion, bool baseDefinitions) {
            var classID = reader.ReadInt32();
            if (fileFormatVersion > 15) {
                reader.ReadByte();
                var type1 = (int)reader.ReadInt16();
                if (type1 >= 0) {
                    type1 = -(1 + type1);
                } else {
                    type1 = classID;
                }

                var classIDs = _classIDs ?? (_classIDs = new List<ClassID>());
                classIDs.Add(new ClassID(type1, classID));

                if (classID == 114) {
                    reader.Position += 16;
                }

                classID = type1;
            } else if (classID < 0) {
                reader.Position += 16;
            }
            reader.Position += 16;

            if (baseDefinitions) {
                var varCount = reader.ReadInt32();
                var stringSize = reader.ReadInt32();

                reader.Position += varCount * 24;

                var stringBlockData = reader.ReadBytes(stringSize);

                var classVars = new List<ClassMember>();
                reader.Position -= varCount * 24 + stringSize;

                string baseName = null, baseType = null;
                int baseSize = 0, baseIndex = 0, baseFlags = 0;

                using (var stringBlockMemory = new MemoryStream(stringBlockData, false)) {
                    using (var stringReader = new EndianBinaryReader(stringBlockMemory, Endian.BigEndian)) {
                        for (var i = 0; i < varCount; ++i) {
                            var dummy = reader.ReadInt16();
                            var level = reader.ReadByte();
                            var isArray = reader.ReadBoolean();

                            var varTypeIndex = reader.ReadUInt16();
                            var isUserDefinedName = reader.ReadUInt16();
                            string varTypeStr;

                            if (isUserDefinedName == 0) {
                                stringReader.Position = varTypeIndex;
                                varTypeStr = stringReader.ReadStringToNull();
                            } else {
                                varTypeStr = CommonStrings.ContainsKey(varTypeIndex) ? CommonStrings[varTypeIndex] : varTypeIndex.ToString();
                            }

                            var varNameIndex = reader.ReadUInt16();
                            isUserDefinedName = reader.ReadUInt16();
                            string varNameStr;

                            if (isUserDefinedName == 0) {
                                stringReader.Position = varNameIndex;
                                varNameStr = stringReader.ReadStringToNull();
                            } else {
                                varNameStr = CommonStrings.ContainsKey(varNameIndex) ? CommonStrings[varNameIndex] : varNameIndex.ToString();
                            }

                            var size = reader.ReadInt32();
                            var index = reader.ReadInt32();
                            var flags = reader.ReadInt32();

                            if (index == 0) {
                                baseName = varNameStr;
                                baseType = varTypeStr;
                                baseSize = size;
                                baseIndex = index;
                                baseFlags = flags;
                            } else {
                                var member = new ClassMember(level - 1, baseType, baseName, varTypeStr, varNameStr, size, flags, null);
                                classVars.Add(member);
                            }
                        }
                    }
                }

                var baseMember = new ClassMember(0, string.Empty, string.Empty, baseType, baseName, baseSize, baseFlags, classVars);
                _objects[classID] = baseMember;

                reader.Position += stringSize;
            }
        }

        private void ReadAssetsPreloadTable(EndianBinaryReader reader, int fileFormatVersion, uint dataOffset) {
            var assetCount = reader.ReadInt32();
            var assetUniqueIDFormat = "D" + MathHelper.GetDigitCount(assetCount);

            var preloadDataList = new AssetPreloadData[assetCount];
            for (var i = 0; i < assetCount; ++i) {
                if (fileFormatVersion >= 14) {
                    reader.AlignBy(4);
                }

                var preloadData = new AssetPreloadData(this);
                preloadDataList[i] = preloadData;

                if (fileFormatVersion < 14) {
                    preloadData.PathID = reader.ReadInt32();
                } else {
                    preloadData.PathID = reader.ReadInt64();
                }

                var offset = reader.ReadUInt32();
                offset += dataOffset;
                preloadData.Offset = offset;

                preloadData.Size = reader.ReadInt32();

                int type1;
                int type2;
                if (fileFormatVersion > 15) {
                    var index = reader.ReadInt32();
                    var t = _classIDs[index];
                    type1 = t.Type1;
                    type2 = t.Type2;
                } else {
                    type1 = reader.ReadInt32();
                    type2 = reader.ReadUInt16();
                    reader.Position += 2;
                }

                preloadData.Type1 = type1;
                preloadData.Type2 = type2;

                if (fileFormatVersion == 15) {
                    var dummyByte = reader.ReadByte();
                    if (dummyByte == 0) {
                    }
                }

                if (ClassIDReference.TryGetClassName(type2, out var typeName)) {
                    preloadData.TypeName = typeName;
                } else {
                    preloadData.TypeName = $"#{type2}";
                }

                preloadData.UniqueID = i.ToString(assetUniqueIDFormat);

                if (fileFormatVersion == 6 && type2 == 141) {
                    var nextAssetPosition = reader.Position;
                    var buildSettings = new BuildSettings(reader, preloadData.Offset, fileFormatVersion, VersionComponents, BuildType);
                    Version = buildSettings.Version;
                    reader.Position = nextAssetPosition;
                }
            }

            PreloadDataList = preloadDataList;
        }

        private List<ClassID> _classIDs;

        private readonly Dictionary<int, ClassMember> _objects = new Dictionary<int, ClassMember>();

        internal static readonly Regex VersionSplitter = new Regex(@"[a-z.]");
        internal static readonly Regex BuildTypePattern = new Regex(@"(\d+\.)+\d+(?<build>[A-Za-z]+)\d+");

        private struct ClassID {

            public ClassID(int type1, int type2) {
                Type1 = type1;
                Type2 = type2;
            }

            public int Type1;

            public int Type2;

        }

        private static readonly IReadOnlyDictionary<int, string> CommonStrings = new Dictionary<int, string> {
            [0] = "AABB",
            [5] = "AnimationClip",
            [19] = "AnimationCurve",
            [34] = "AnimationState",
            [49] = "Array",
            [55] = "Base",
            [60] = "BitField",
            [69] = "bitset",
            [76] = "bool",
            [81] = "char",
            [86] = "ColorRGBA",
            [96] = "Component",
            [106] = "data",
            [111] = "deque",
            [117] = "double",
            [124] = "dynamic_array",
            [138] = "FastPropertyName",
            [155] = "first",
            [161] = "float",
            [167] = "Font",
            [172] = "GameObject",
            [183] = "Generic Mono",
            [196] = "GradientNEW",
            [208] = "GUID",
            [213] = "GUIStyle",
            [222] = "int",
            [226] = "list",
            [231] = "long long",
            [241] = "map",
            [245] = "Matrix4x4f",
            [256] = "MdFour",
            [263] = "MonoBehaviour",
            [277] = "MonoScript",
            [288] = "m_ByteSize",
            [299] = "m_Curve",
            [307] = "m_EditorClassIdentifier",
            [331] = "m_EditorHideFlags",
            [349] = "m_Enabled",
            [359] = "m_ExtensionPtr",
            [374] = "m_GameObject",
            [387] = "m_Index",
            [395] = "m_IsArray",
            [405] = "m_IsStatic",
            [416] = "m_MetaFlag",
            [427] = "m_Name",
            [434] = "m_ObjectHideFlags",
            [452] = "m_PrefabInternal",
            [469] = "m_PrefabParentObject",
            [490] = "m_Script",
            [499] = "m_StaticEditorFlags",
            [519] = "m_Type",
            [526] = "m_Version",
            [536] = "Object",
            [543] = "pair",
            [548] = "PPtr<Component>",
            [564] = "PPtr<GameObject>",
            [581] = "PPtr<Material>",
            [596] = "PPtr<MonoBehaviour>",
            [616] = "PPtr<MonoScript>",
            [633] = "PPtr<Object>",
            [646] = "PPtr<Prefab>",
            [659] = "PPtr<Sprite>",
            [672] = "PPtr<TextAsset>",
            [688] = "PPtr<Texture>",
            [702] = "PPtr<Texture2D>",
            [718] = "PPtr<Transform>",
            [734] = "Prefab",
            [741] = "Quaternionf",
            [753] = "Rectf",
            [759] = "RectInt",
            [767] = "RectOffset",
            [778] = "second",
            [785] = "set",
            [789] = "short",
            [795] = "size",
            [800] = "SInt16",
            [807] = "SInt32",
            [814] = "SInt64",
            [821] = "SInt8",
            [827] = "staticvector",
            [840] = "string",
            [847] = "TextAsset",
            [857] = "TextMesh",
            [866] = "Texture",
            [874] = "Texture2D",
            [884] = "Transform",
            [894] = "TypelessData",
            [907] = "UInt16",
            [914] = "UInt32",
            [921] = "UInt64",
            [928] = "UInt8",
            [934] = "unsigned int",
            [947] = "unsigned long long",
            [966] = "unsigned short",
            [981] = "vector",
            [988] = "Vector2f",
            [997] = "Vector3f",
            [1006] = "Vector4f",
            [1015] = "m_ScriptingClassIdentifier",
            [1042] = "Gradient",
            [1051] = "Type*"
        };

    }
}
