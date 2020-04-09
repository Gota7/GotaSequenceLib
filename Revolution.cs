using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// Wii platform.
    /// </summary>
    public class Revolution : SequencePlatform {

        /// <summary>
        /// Command map.
        /// </summary>
        /// <returns>The commands mapped.</returns>
        public override Dictionary<SequenceCommands, byte> CommandMap() => new Dictionary<SequenceCommands, byte>() {
            { SequenceCommands.Wait, 0x80 },
            { SequenceCommands.ProgramChange, 0x81 },
            { SequenceCommands.OpenTrack, 0x88 },
            { SequenceCommands.Jump, 0x89 },
            { SequenceCommands.Call, 0x8A },
            { SequenceCommands.Random, 0xA0 },
            { SequenceCommands.Variable, 0xA1 },
            { SequenceCommands.If, 0xA2 },
            { SequenceCommands.Time, 0xA3 },
            { SequenceCommands.TimeRandom, 0xA4 },
            { SequenceCommands.TimeVariable, 0xA5 },
            { SequenceCommands.Timebase, 0xB0 },
            { SequenceCommands.EnvHold, 0xB1 },
            { SequenceCommands.Monophonic, 0xB2 },
            { SequenceCommands.VelocityRange, 0xB3 },
            { SequenceCommands.BiquadType, 0xB4 },
            { SequenceCommands.BiquadValue, 0xB5 },
            { SequenceCommands.Pan, 0xC0 },
            { SequenceCommands.Volume, 0xC1 },
            { SequenceCommands.MainVolume, 0xC2 },
            { SequenceCommands.Transpose, 0xC3 },
            { SequenceCommands.PitchBend, 0xC4 },
            { SequenceCommands.BendRange, 0xC5 },
            { SequenceCommands.Prio, 0xC6 },
            { SequenceCommands.NoteWait, 0xC7 },
            { SequenceCommands.Tie, 0xC8 },
            { SequenceCommands.Porta, 0xC9 },
            { SequenceCommands.ModDepth, 0xCA },
            { SequenceCommands.ModSpeed, 0xCB },
            { SequenceCommands.ModType, 0xCC },
            { SequenceCommands.ModRange, 0xCD },
            { SequenceCommands.PortaSw, 0xCE },
            { SequenceCommands.PortaTime, 0xCF },
            { SequenceCommands.Attack, 0xD0 },
            { SequenceCommands.Decay, 0xD1 },
            { SequenceCommands.Sustain, 0xD2 },
            { SequenceCommands.Release, 0xD3 },
            { SequenceCommands.LoopStart, 0xD4 },
            { SequenceCommands.Volume2, 0xD5 },
            { SequenceCommands.PrintVar, 0xD6 },
            { SequenceCommands.SurroundPan, 0xD7 },
            { SequenceCommands.LpfCutoff, 0xD8 },
            { SequenceCommands.FxSendA, 0xD9 },
            { SequenceCommands.FxSendB, 0xDA },
            { SequenceCommands.MainSend, 0xDB },
            { SequenceCommands.InitPan, 0xDC },
            { SequenceCommands.Mute, 0xDD },
            { SequenceCommands.FxSendC, 0xDE },
            { SequenceCommands.Damper, 0xDF },
            { SequenceCommands.ModDelay, 0xE0 },
            { SequenceCommands.Tempo, 0xE1 },
            { SequenceCommands.SweepPitch, 0xE3 },
            { SequenceCommands.Extended, 0xF0 },
            { SequenceCommands.EnvReset, 0xFB },
            { SequenceCommands.LoopEnd, 0xFC },
            { SequenceCommands.Return, 0xFD },
            { SequenceCommands.AllocateTrack, 0xFE },
            { SequenceCommands.Fin, 0xFF },
        };

        /// <summary>
        /// Extended commands.
        /// </summary>
        /// <returns>The extended commands mapped.</returns>
        public override Dictionary<SequenceCommands, byte> ExtendedCommands() => new Dictionary<SequenceCommands, byte>() {
            { SequenceCommands.SetVar, 0x80 },
            { SequenceCommands.AddVar, 0x81 },
            { SequenceCommands.SubVar, 0x82 },
            { SequenceCommands.MulVar, 0x83 },
            { SequenceCommands.DivVar, 0x84 },
            { SequenceCommands.ShiftVar, 0x85 },
            { SequenceCommands.RandVar, 0x86 },
            { SequenceCommands.AndVar, 0x87 },
            { SequenceCommands.OrVar, 0x88 },
            { SequenceCommands.XorVar, 0x89 },
            { SequenceCommands.NotVar, 0x8A },
            { SequenceCommands.ModVar, 0x8B },
            { SequenceCommands.CmpEq, 0x90 },
            { SequenceCommands.CmpGe, 0x91 },
            { SequenceCommands.CmpGt, 0x92 },
            { SequenceCommands.CmpLe, 0x93 },
            { SequenceCommands.CmpLt, 0x94 },
            { SequenceCommands.CmpNe, 0x95 },
            { SequenceCommands.UserCall, 0xE0 }
        };

        /// <summary>
        /// Sequence data byte order.
        /// </summary>
        /// <returns>The byte order of sequence data.</returns>
        public override ByteOrder SequenceDataByteOrder() => ByteOrder.BigEndian;

    }

}
