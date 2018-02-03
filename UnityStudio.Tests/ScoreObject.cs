using UnityStudio.Serialization;
using UnityStudio.Serialization.Naming;

namespace UnityStudio.Tests {
    [MonoBehavior(NamingConventionType = typeof(CamelCaseNamingConvention))]
    public sealed class ScoreObject {

        [MonoBehaviorProperty(Name = "evts")]
        public EventNoteData[] NoteEvents { get; set; }

        [MonoBehaviorProperty(Name = "ct")]
        public EventConductorData[] ConductorEvents { get; set; }

        public float[] JudgeRange { get; set; }

        public float[] ScoreSpeed { get; set; }

        [MonoBehaviorProperty(Name = "BGM_offset")]
        public float BgmOffset { get; set; }

        [MonoBehavior(NamingConventionType = typeof(CamelCaseNamingConvention))]
        public sealed class EventNoteData {

            [MonoBehaviorProperty(Name = "absTime", ConverterType = typeof(DoubleToSingleConverter))]
            public float AbsoluteTime { get; set; }

            public bool Selected { get; set; }

            public long Tick { get; set; }

            public int Measure { get; set; }

            public int Beat { get; set; }

            public int Track { get; set; }

            public int Type { get; set; }

            [MonoBehaviorProperty(Name = "startPosx")]
            public float StartPositionX { get; set; }

            [MonoBehaviorProperty(Name = "endPosx")]
            public float EndPositionX { get; set; }

            public float Speed { get; set; }

            public int Duration { get; set; }

            [MonoBehaviorProperty(Name = "poly")]
            public PolyPoint[] Polypoints { get; set; }

            public int EndType { get; set; }

            [MonoBehaviorProperty(ConverterType = typeof(DoubleToSingleConverter))]
            public float LeadTime { get; set; }

            [MonoBehavior(NamingConventionType = typeof(CamelCaseNamingConvention))]
            public sealed class PolyPoint {

                [MonoBehaviorProperty(Name = "subtick")]
                public int SubTick { get; set; }

                [MonoBehaviorProperty(Name = "posx")]
                public float PositionX { get; set; }

            }

        }

        [MonoBehavior(NamingConventionType = typeof(CamelCaseNamingConvention))]
        public sealed class EventConductorData {

            [MonoBehaviorProperty(Name = "absTime", ConverterType = typeof(DoubleToSingleConverter))]
            public float AbsoluteTime { get; set; }

            public bool Selected { get; set; }

            public long Tick { get; set; }

            public int Measure { get; set; }

            public int Beat { get; set; }

            public int Track { get; set; }

            [MonoBehaviorProperty(ConverterType = typeof(DoubleToSingleConverter))]
            public float Tempo { get; set; }

            [MonoBehaviorProperty(Name = "tsigNumerator")]
            public int SignatureNumerator { get; set; }

            [MonoBehaviorProperty(Name = "tsigDenominator")]
            public int SignatureDenominator { get; set; }

            public string Marker { get; set; }

        }

    }
}
