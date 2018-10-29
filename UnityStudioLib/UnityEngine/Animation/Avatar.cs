using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityStudio.Extensions;
using UnityStudio.Models;

namespace UnityStudio.UnityEngine.Animation {
    public class Avatar {

        private protected Avatar() {
            AvatarSkeleton = Skeleton.CreateEmpty();
            AvatarSkeletonPose = SkeletonPose.CreateEmpty();
            DefaultPose = SkeletonPose.CreateEmpty();
            SkeletonNameIDs = new uint[0];
            Human = new Human();
            HumanSkeletonIndices = new int[0];
            HumanSkeletonReverseIndices = new int[0];
            RootMotionBoneTransform = new Transform();
            RootMotionSkeleton = Skeleton.CreateEmpty();
            RootMotionSkeletonPose = SkeletonPose.CreateEmpty();
            RootMotionSkeletonIndices = new int[0];
        }

        [NotNull]
        public virtual string Name { get; protected set; } = string.Empty;

        public uint Size { get; internal set; }

        [NotNull]
        public Skeleton AvatarSkeleton { get; internal set; }

        [NotNull]
        public SkeletonPose AvatarSkeletonPose { get; internal set; }

        [NotNull]
        public SkeletonPose DefaultPose { get; internal set; }

        [NotNull]
        public uint[] SkeletonNameIDs { get; internal set; }

        [NotNull]
        public Human Human { get; private set; }

        [NotNull]
        public int[] HumanSkeletonIndices { get; private set; }

        [NotNull]
        public int[] HumanSkeletonReverseIndices { get; private set; }

        public int RootMotionBoneIndex { get; private set; } = -1;

        [NotNull]
        public Transform RootMotionBoneTransform { get; private set; }

        [NotNull]
        public Skeleton RootMotionSkeleton { get; private set; }

        [NotNull]
        public SkeletonPose RootMotionSkeletonPose { get; private set; }

        [NotNull]
        public int[] RootMotionSkeletonIndices { get; private set; }

        [NotNull]
        public IReadOnlyDictionary<uint, string> BoneNamesMap => _boneNamesMap;

        [NotNull]
        internal static Avatar ReadFromAssetPreloadData([NotNull] AssetPreloadData preloadData) {
            var sourceFile = preloadData.Source;
            var version = sourceFile.VersionComponents;
            var reader = sourceFile.FileReader;
            var avatar = new Avatar();

            reader.Position = preloadData.Offset;

            avatar.Name = reader.ReadAlignedString();
            avatar.Size = reader.ReadUInt32();

            avatar.AvatarSkeleton = ReadSkeleton();
            avatar.AvatarSkeletonPose = ReadPose();

            if (version[0] > 4 || (version[0] == 4 && version[1] >= 3)) {
                avatar.DefaultPose = ReadPose();

                var idCount = reader.ReadInt32();

                avatar.SkeletonNameIDs = new uint[idCount];

                for (var i = 0; i < idCount; ++i) {
                    avatar.SkeletonNameIDs[i] = reader.ReadUInt32();
                }
            }

            avatar.Human = ReadHuman();

            reader.AlignBy(4);

            avatar.HumanSkeletonIndices = ReadArray(reader.ReadInt32);

            if (version[0] > 4 || (version[0] == 4 && version[1] >= 3)) {
                avatar.HumanSkeletonReverseIndices = ReadArray(reader.ReadInt32);
            }

            avatar.RootMotionBoneIndex = reader.ReadInt32();

            avatar.RootMotionBoneTransform = ReadTransform();

            if (version[0] > 4 || (version[0] == 4 && version[1] >= 3)) {
                avatar.RootMotionSkeleton = ReadSkeleton();
                avatar.RootMotionSkeletonPose = ReadPose();
                avatar.RootMotionSkeletonIndices = ReadArray(reader.ReadInt32);
            }

            var nameMapEntryCount = reader.ReadInt32();

            for (var i = 0; i < nameMapEntryCount; ++i) {
                var key = reader.ReadUInt32();
                var value = reader.ReadAlignedString();
                avatar._boneNamesMap[key] = value;
            }

            return avatar;

            Skeleton ReadSkeleton() {
                var skeleton = new Skeleton();

                var nodeCount = reader.ReadInt32();

                skeleton.Nodes = new SkeletonNode[nodeCount];

                for (var i = 0; i < nodeCount; ++i) {
                    var parentId = reader.ReadInt32();
                    var axesId = reader.ReadInt32();

                    skeleton.Nodes[i] = new SkeletonNode(parentId, axesId);
                }

                var idCount = reader.ReadInt32();

                skeleton.NodeIDs = new uint[idCount];

                for (var i = 0; i < idCount; ++i) {
                    skeleton.NodeIDs[i] = reader.ReadUInt32();
                }

                var axesCount = reader.ReadInt32();

                skeleton.Axes = new object[axesCount];

                for (var i = 0; i < axesCount; ++i) {
                    if (version[0] > 5 || (version[0] == 5 && version[1] >= 4)) {
                        reader.Position += 76;
                    } else {
                        reader.Position += 88;
                    }
                }

                return skeleton;
            }

            SkeletonPose ReadPose() {
                var pose = new SkeletonPose();

                var transformCount = reader.ReadInt32();

                pose.Transforms = new Transform[transformCount];

                for (var i = 0; i < transformCount; ++i) {
                    pose.Transforms[i] = ReadTransform();
                }

                return pose;
            }

            Human ReadHuman() {
                var human = new Human();

                human.RootTransform = ReadTransform();
                human.Skeleton = ReadSkeleton();
                human.SkeletonPose = ReadPose();
                human.LeftHand = ReadHand();
                human.RightHand = ReadHand();

                if (version[0] < 2018 || (version[0] == 2018 && version[1] < 2)) {
                    var handleCount = reader.ReadInt32();

                    human.Handles = new object[handleCount];

                    for (var i = 0; i < handleCount; ++i) {
                        if (version[0] > 5 || (version[0] == 5 && version[1] >= 4)) {
                            reader.Position += 48;
                        } else {
                            reader.Position += 56;
                        }
                    }

                    var colliderCount = reader.ReadInt32();

                    human.Colliders = new object[colliderCount];

                    for (var i = 0; i < colliderCount; ++i) {
                        if (version[0] > 5 || (version[0] == 5 && version[1] >= 4)) {
                            reader.Position += 72;
                        } else {
                            reader.Position += 80;
                        }
                    }
                }

                human.HumanBoneIndices = ReadArray(reader.ReadInt32);

                var massCount = reader.ReadInt32();

                human.HumanBoneMasses = new float[massCount];

                for (var i = 0; i < massCount; ++i) {
                    human.HumanBoneMasses[i] = reader.ReadSingle();
                }

                if (version[0] < 2018 || (version[0] == 2018 && version[1] < 2)) {
                    human.ColliderIndices = ReadArray(reader.ReadInt32);
                }

                human.Scale = reader.ReadSingle();
                human.ArmTwist = reader.ReadSingle();
                human.ForeArmTwist = reader.ReadSingle();
                human.UpperLegTwist = reader.ReadSingle();
                human.LegTwist = reader.ReadSingle();
                human.ArmStretch = reader.ReadSingle();
                human.LegStretch = reader.ReadSingle();
                human.FeetSpacing = reader.ReadSingle();
                human.HasLeftHand = reader.ReadBoolean();
                human.HasRightHand = reader.ReadBoolean();
                human.HasTDoF = reader.ReadBoolean();

                return human;
            }

            Hand ReadHand() {
                var hand = new Hand();

                var fingerIndexCount = reader.ReadInt32();

                for (var i = 0; i < fingerIndexCount; ++i) {
                    hand[i] = reader.ReadInt32();
                }

                return hand;
            }

            Transform ReadTransform() {
                Transform transform;

                if (version[0] > 5 || (version[0] == 5 && version[1] >= 4)) {
                    var t = reader.ReadVector3();
                    var q = reader.ReadQuaternion();
                    var s = reader.ReadVector3();

                    transform = new Transform(t, q, s);
                } else {
                    var t = reader.ReadVector4();
                    var q = reader.ReadQuaternion();
                    var s = reader.ReadVector4();

                    transform = new Transform {
                        Translation = new Vector3(t.X, t.Y, t.Z),
                        Rotation = q,
                        Scale = new Vector3(s.X, s.Y, s.Z)
                    };
                }

                return transform;
            }

            T[] ReadArray<T>(Func<T> readFunc) {
                var indexCount = reader.ReadInt32();
                var result = new T[indexCount];

                for (var i = 0; i < indexCount; ++i) {
                    result[i] = readFunc();
                }

                return result;
            }
        }

        private protected readonly Dictionary<uint, string> _boneNamesMap = new Dictionary<uint, string>();

    }
}
