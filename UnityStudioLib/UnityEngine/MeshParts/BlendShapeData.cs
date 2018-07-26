using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityStudio.UnityEngine.MeshParts {
    public sealed class BlendShapeData {

        internal BlendShapeData([NotNull, ItemNotNull] IReadOnlyList<BlendShapeVertex> vertices, [NotNull, ItemNotNull] IReadOnlyList<MeshBlendShape> shapes, [NotNull, ItemNotNull] IReadOnlyList<MeshBlendShapeChannel> channels, [NotNull] IReadOnlyList<float> fullWeights) {
            Vertices = vertices;
            Shapes = shapes;
            Channels = channels;
            FullWeights = fullWeights;
        }

        [NotNull, ItemNotNull]
        public IReadOnlyList<BlendShapeVertex> Vertices { get; }

        [NotNull, ItemNotNull]
        public IReadOnlyList<MeshBlendShape> Shapes { get; }

        [NotNull, ItemNotNull]
        public IReadOnlyList<MeshBlendShapeChannel> Channels { get; }

        [NotNull]
        public IReadOnlyList<float> FullWeights { get; }

    }
}
