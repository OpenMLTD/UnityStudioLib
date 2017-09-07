using System.Collections.Generic;

namespace UnityStudio.Models {
    public sealed class ClassMember {

        internal ClassMember(int level, string baseTypeName, string baseName, string typeName, string name, int size, int flags, IReadOnlyList<ClassMember> children) {
            Level = level;
            BaseTypeName = baseTypeName;
            BaseName = baseName;
            TypeName = typeName;
            Name = name;
            Size = size;
            Flags = flags;
            Children = children ?? new ClassMember[0];
        }

        public int Level { get; }

        public string BaseTypeName { get; }

        public string BaseName { get; }

        public string TypeName { get; }

        public string Name { get; }

        public int Size { get; }

        public int Flags { get; }

        public IReadOnlyList<ClassMember> Children { get; }

        public override string ToString() {
            return $"{Name}: {TypeName} @ Level {Level}";
        }

    }
}
