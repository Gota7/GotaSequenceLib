using System;
namespace GotaSequenceLib.Playback {
    public abstract class AbstractTrack {
        public readonly byte Index;
        protected readonly Player _player;


        // Track state
        public bool Allocated;
        public bool Enabled;
        public bool Stopped;
        public int CurEvent;
        public bool VariableFlag;
        public int Rest;
        public int[] CallStack = new int[3];
        public byte[] CallStackLoops = new byte[3];
        public byte CallStackDepth;
        public bool WaitingForNoteToFinishBeforeContinuingXD; // Is this necessary?
        public bool NoteDown;

        // *SEQ properties 
        abstract public bool Tie { set; }
        abstract public bool NoteWait { set; } // previously known as Mono
        abstract public bool Portamento { set; }
        abstract public int Voice { set; }
        abstract public byte Priority { set; }
        abstract public byte Volume { set; }
        abstract public byte Expression { set; }
        abstract public byte LFORange { set; }
        abstract public byte PitchBendRange { set; }
        abstract public byte LFOSpeed { set; }
        abstract public byte LFODepth { set; }
        abstract public ushort LFODelay { set; }
        abstract public ushort LFOPhase { set; }
        abstract public ushort LFODelayCount { set; }
        abstract public LFOType LFOType { set; }
        abstract public sbyte PitchBend { set; }
        abstract public sbyte Panpot { set; }
        abstract public sbyte Transpose { set; }
        abstract public byte Attack { set; }
        abstract public byte Decay { set; }
        abstract public byte Sustain { set; }
        abstract public byte Hold { set; }
        abstract public byte Release { set; }
        abstract public byte PortamentoKey { set; }
        abstract public byte PortamentoTime { set; }
        abstract public short SweepPitch { set; }
        abstract public int BankNum { set; }

        public class TrackVars {
            private readonly Player _player;
            private readonly short[] _trackVars = new short[0x10];

            public short this[int i] {
                get {
                    if (i < 0x20) {
                        return _player.Vars[i];
                    } else {
                        return _trackVars[i - 0x20];
                    }
                }
                set {
                    if (i < 0x20) {
                        _player.Vars[i] = value;
                    } else {
                        _trackVars[i - 0x20] = value;
                    }
                }
            }

            internal TrackVars(Player player) {
                _player = player;
            }
        }

        public readonly TrackVars Vars;

        protected AbstractTrack(byte idx, Player player) {
            Index = idx;
            _player = player;
            Vars = new TrackVars(player);
        }


    }

    // public enum LFOType
    // {
    //     Pitch,
    //     Volume,
    //     Panpot
    // }
}
