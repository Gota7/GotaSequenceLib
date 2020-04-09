using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// Nintendo DS.
    /// </summary>
    public class Nitro : SequencePlatform {
        /// <summary>
        /// Command map.
        /// </summary>
        /// <returns>The commands mapped.</returns>
        public override Dictionary<SequenceCommands, byte> CommandMap() => new Dictionary<SequenceCommands, byte>() {
            { SequenceCommands.Wait, 0x80 },
            { SequenceCommands.ProgramChange, 0x81 },
            { SequenceCommands.OpenTrack, 0x93 },
            { SequenceCommands.Jump, 0x94 },
            { SequenceCommands.Call, 0x95 },
            { SequenceCommands.Random, 0xA0 },
            { SequenceCommands.Variable, 0xA1 },
            { SequenceCommands.If, 0xA2 },
            { SequenceCommands.SetVar, 0xB0 },
            { SequenceCommands.AddVar, 0xB1 },
            { SequenceCommands.SubVar, 0xB2 },
            { SequenceCommands.MulVar, 0xB3 },
            { SequenceCommands.DivVar, 0xB4 },
            { SequenceCommands.ShiftVar, 0xB5 },
            { SequenceCommands.RandVar, 0xB6 },
            { SequenceCommands.CmpEq, 0xB8 },
            { SequenceCommands.CmpGe, 0xB9 },
            { SequenceCommands.CmpGt, 0xBA },
            { SequenceCommands.CmpLe, 0xBB },
            { SequenceCommands.CmpLt, 0xBC },
            { SequenceCommands.CmpNe, 0xBD },
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
            { SequenceCommands.ModDelay, 0xE0 },
            { SequenceCommands.Tempo, 0xE1 },
            { SequenceCommands.SweepPitch, 0xE3 },
            { SequenceCommands.LoopEnd, 0xFC },
            { SequenceCommands.Return, 0xFD },
            { SequenceCommands.AllocateTrack, 0xFE },
            { SequenceCommands.Fin, 0xFF },
        };

        /// <summary>
        /// Extended commands.
        /// </summary>
        /// <returns>The extended commands mapped.</returns>
        public override Dictionary<SequenceCommands, byte> ExtendedCommands() => new Dictionary<SequenceCommands, byte>() {};

        /// <summary>
        /// Sequence data byte order.
        /// </summary>
        /// <returns>The byte order of sequence data.</returns>
        public override ByteOrder SequenceDataByteOrder() => ByteOrder.LittleEndian;

    }

}
