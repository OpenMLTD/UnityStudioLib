using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using UnityStudio.UnityEngine;
using UnityStudio.UnityEngine.Animation;

namespace UnityStudio.Utilities {
    public sealed class CompositeAvatar : Avatar {

        private CompositeAvatar() {
        }

        [NotNull, ItemNotNull]
        public IReadOnlyList<string> Names { get; private set; }

        public override string Name => _name;

        private void UpdateName() {
            var sb = new StringBuilder();

            sb.Append("Composite avatar of:");

            foreach (var name in Names) {
                sb.AppendLine();
                sb.Append("\t");
                sb.AppendLine(name);
            }

            _name = sb.ToString();
        }

        private string _name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]
        public static CompositeAvatar FromAvatars([NotNull, ItemNotNull] params Avatar[] avatars) {
            return FromAvatars(avatars as IReadOnlyList<Avatar>);
        }

        [NotNull]
        public static CompositeAvatar FromAvatars([NotNull, ItemNotNull] IReadOnlyList<Avatar> avatars) {
            var result = new CompositeAvatar();

            result.Size = (uint)avatars.Sum(av => av.Size);
            result.Names = avatars.Select(av => av.Name).ToArray();

            result.UpdateName();

            result.AvatarSkeleton = CombineSkeletons(avatars.Select(av => av.AvatarSkeleton));
            result.AvatarSkeletonPose = CombineSkeletonPoses(avatars.Select(av => av.AvatarSkeletonPose), avatars.Select(av => av.AvatarSkeleton));
            result.DefaultPose = CombineSkeletonPoses(avatars.Select(av => av.DefaultPose), avatars.Select(av => av.AvatarSkeleton));

            result.SkeletonNameIDs = avatars.SelectMany(av => av.SkeletonNameIDs).ToArray();

            foreach (var avatar in avatars) {
                foreach (var kv in avatar.BoneNamesMap) {
                    if (!result._boneNamesMap.ContainsKey(kv.Key)) {
                        result._boneNamesMap.Add(kv.Key, kv.Value);
                    }
                }
            }

            return result;
        }

        private static Skeleton CombineSkeletons([NotNull, ItemNotNull] IEnumerable<Skeleton> skeletons) {
            var nodeIDList = new List<uint>();
            var nodeList = new List<SkeletonNode>();

            var nodeStart = 0;
            var counter = 0;

            foreach (var skeleton in skeletons) {
                Debug.Assert(skeleton.NodeIDs.Length == skeleton.Nodes.Length);

                var nodeCount = skeleton.NodeIDs.Length;

                foreach (var id in skeleton.NodeIDs) {
                    if (id == 0) {
                        if (nodeIDList.Count == 0) {
                            nodeIDList.Add(id);
                        }
                    } else {
                        nodeIDList.Add(id);
                    }
                }

                var isFirstNode = true;

                foreach (var node in skeleton.Nodes) {
                    if (isFirstNode) {
                        if (nodeList.Count > 0) {
                            isFirstNode = false;
                            continue;
                        }
                    }

                    int parentIndex;

                    if (counter == 0) {
                        parentIndex = node.ParentIndex;
                    } else {
                        if (node.ParentIndex <= 0) {
                            parentIndex = node.ParentIndex;
                        } else {
                            parentIndex = node.ParentIndex + nodeStart - 1;
                        }
                    }

                    var n = new SkeletonNode(parentIndex, node.AxisIndex);

                    nodeList.Add(n);

                    isFirstNode = false;
                }

                if (counter == 0) {
                    nodeStart += nodeCount;
                } else {
                    // From the second time, "" (empty name) bone is filtered out because it will duplicate.
                    nodeStart += nodeCount - 1;
                }

                ++counter;
            }

            var result = new Skeleton();

            result.NodeIDs = nodeIDList.ToArray();
            result.Nodes = nodeList.ToArray();

            return result;
        }

        private static SkeletonPose CombineSkeletonPoses([NotNull, ItemNotNull] IEnumerable<SkeletonPose> poses, [NotNull, ItemNotNull] IEnumerable<Skeleton> skeletons) {
            var transformList = new List<Transform>();

            foreach (var (pose, skeleton) in EnumerableUtils.Zip(poses, skeletons)) {
                var transforms = pose.Transforms;
                var ids = skeleton.NodeIDs;

                Debug.Assert(transforms.Length == ids.Length);

                var len = transforms.Length;

                for (var i = 0; i < len; ++i) {
                    if (ids[i] == 0) {
                        if (transformList.Count == 0) {
                            transformList.Add(transforms[i]);
                        }
                    } else {
                        transformList.Add(transforms[i]);
                    }
                }
            }

            var result = new SkeletonPose();

            result.Transforms = transformList.ToArray();

            return result;
        }

    }
}
