using UnityStudio.Serialization;
using UnityStudio.Serialization.Naming;

namespace UnityStudio.Tests.Models {
    [MonoBehaviour(NamingConventionType = typeof(CamelCaseNamingConvention))]
    public sealed class ScoreObjectInFields {

        [MonoBehaviourProperty(Name = "evts")]
        public EventNoteData[] NoteEvents;

        [MonoBehaviourProperty(Name = "ct")]
        public EventConductorData[] ConductorEvents;

        public float[] JudgeRange;

        public float[] ScoreSpeed;

        [MonoBehaviourProperty(Name = "BGM_offset")]
        public float BgmOffset;

        [MonoBehaviour(NamingConventionType = typeof(CamelCaseNamingConvention))]
        public sealed class EventNoteData {

            [MonoBehaviourProperty(Name = "absTime", ConverterType = typeof(DoubleToSingleConverter))]
            public float AbsoluteTime;

            public bool Selected;

            public long Tick;

            public int Measure;

            public int Beat;

            public int Track;

            public int Type;

            [MonoBehaviourProperty(Name = "startPosx")]
            public float StartPositionX;

            [MonoBehaviourProperty(Name = "endPosx")]
            public float EndPositionX;

            public float Speed;

            public int Duration;

            [MonoBehaviourProperty(Name = "poly")]
            public PolyPoint[] Polypoints;

            public int EndType;

            [MonoBehaviourProperty(ConverterType = typeof(DoubleToSingleConverter))]
            public float LeadTime;

            [MonoBehaviour(NamingConventionType = typeof(CamelCaseNamingConvention))]
            public sealed class PolyPoint {

                [MonoBehaviourProperty(Name = "subtick")]
                public int SubTick;

                [MonoBehaviourProperty(Name = "posx")]
                public float PositionX;

            }

        }

        [MonoBehaviour(NamingConventionType = typeof(CamelCaseNamingConvention))]
        public sealed class EventConductorData {

            [MonoBehaviourProperty(Name = "absTime", ConverterType = typeof(DoubleToSingleConverter))]
            public float AbsoluteTime;

            public bool Selected;

            public long Tick;

            public int Measure;

            public int Beat;

            public int Track;

            [MonoBehaviourProperty(ConverterType = typeof(DoubleToSingleConverter))]
            public float Tempo;

            [MonoBehaviourProperty(Name = "tsigNumerator")]
            public int SignatureNumerator;

            [MonoBehaviourProperty(Name = "tsigDenominator")]
            public int SignatureDenominator;

            public string Marker;

        }

    }
}
