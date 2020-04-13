using System;
using Sanford.Multimedia.Midi;
namespace GotaSequenceLib.Playback {
    public class MidiTrack: AbstractTrack {
        private readonly Sanford.Multimedia.Midi.Track _track = new Sanford.Multimedia.Midi.Track();
        // *SEQ properties
        private bool _tie = false;
        override public bool Tie {
            set {
                _tie = value;
                Message(new ChannelMessage(ChannelCommand.Controller, Index, (int)ControllerType.AllNotesOff));
            }
        }

        private bool _noteWait = true;
        override public bool NoteWait { set { _noteWait = value; } } // AKA NoteWait?
        override public bool Portamento { set; }
        override public int Voice { set; }
        override public byte Priority { set; }
        override public byte Volume { set; }
        override public byte Expression { set; }
        override public byte LFORange { set; }
        override public byte PitchBendRange { set; }
        override public byte LFOSpeed { set; }
        override public byte LFODepth { set; }
        override public ushort LFODelay { set; }
        override public ushort LFOPhase { set; }
        override public ushort LFODelayCount { set; }
        override public LFOType LFOType { set; }
        override public sbyte PitchBend { set; }
        override public sbyte Panpot { set; }
        override public sbyte Transpose { set; }
        override public byte Attack { set; }
        override public byte Decay { set; }
        override public byte Sustain { set; }
        override public byte Hold { set; }
        override public byte Release { set; }
        override public byte PortamentoKey { set; }
        override public byte PortamentoTime { set; }
        override public short SweepPitch { set; }
        override public int BankNum { set; }

        private void Message(ChannelMessage message) {
            _track.Insert((int)_player.ElapsedTicks, message);
        }

        public MidiTrack(byte idx, Player player): base(idx, player) {
            
        }
    }
}
