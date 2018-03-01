using UnityStudio.Serialization;
using UnityStudio.Serialization.Naming;

namespace UnityStudio.Tests {
    [MonoBehaviour(NamingConventionType = typeof(CamelCaseNamingConvention))]
    public sealed class ScoreObject {

        [MonoBehaviourProperty(Name = "evts")]
        public EventNoteData[] NoteEvents { get; set; }

        [MonoBehaviourProperty(Name = "ct")]
        public EventConductorData[] ConductorEvents { get; set; }

        public float[] JudgeRange { get; set; }

        public float[] ScoreSpeed { get; set; }

        [MonoBehaviourProperty(Name = "BGM_offset")]
        public float BgmOffset { get; set; }

        [MonoBehaviour(NamingConventionType = typeof(CamelCaseNamingConvention))]
        public sealed class EventNoteData {

            [MonoBehaviourProperty(Name = "absTime", ConverterType = typeof(DoubleToSingleConverter))]
            public float AbsoluteTime { get; set; }

            public bool Selected { get; set; }

            public long Tick { get; set; }

            public int Measure { get; set; }

            public int Beat { get; set; }

            public int Track { get; set; }

            public int Type { get; set; }

            [MonoBehaviourProperty(Name = "startPosx")]
            public float StartPositionX { get; set; }

            [MonoBehaviourProperty(Name = "endPosx")]
            public float EndPositionX { get; set; }

            public float Speed { get; set; }

            public int Duration { get; set; }

            [MonoBehaviourProperty(Name = "poly")]
            public PolyPoint[] Polypoints { get; set; }

            public int EndType { get; set; }

            [MonoBehaviourProperty(ConverterType = typeof(DoubleToSingleConverter))]
            public float LeadTime { get; set; }

            [MonoBehaviour(NamingConventionType = typeof(CamelCaseNamingConvention))]
            public sealed class PolyPoint {

                [MonoBehaviourProperty(Name = "subtick")]
                public int SubTick { get; set; }

                [MonoBehaviourProperty(Name = "posx")]
                public float PositionX { get; set; }

            }

        }

        [MonoBehaviour(NamingConventionType = typeof(CamelCaseNamingConvention))]
        public sealed class EventConductorData {

            [MonoBehaviourProperty(Name = "absTime", ConverterType = typeof(DoubleToSingleConverter))]
            public float AbsoluteTime { get; set; }

            public bool Selected { get; set; }

            public long Tick { get; set; }

            public int Measure { get; set; }

            public int Beat { get; set; }

            public int Track { get; set; }

            [MonoBehaviourProperty(ConverterType = typeof(DoubleToSingleConverter))]
            public float Tempo { get; set; }

            [MonoBehaviourProperty(Name = "tsigNumerator")]
            public int SignatureNumerator { get; set; }

            [MonoBehaviourProperty(Name = "tsigDenominator")]
            public int SignatureDenominator { get; set; }

            public string Marker { get; set; }

        }

    }
}
