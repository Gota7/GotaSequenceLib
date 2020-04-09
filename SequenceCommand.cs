using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// Sequence command.
    /// </summary>
    public class SequenceCommand {

        /// <summary>
        /// Command type.
        /// </summary>
        public SequenceCommands CommandType;

        /// <summary>
        /// Parameter.
        /// </summary>
        public object Parameter;

        /// <summary>
        /// Ticks.
        /// </summary>
        public long[] Ticks = new long[0x10];

        /// <summary>
        /// The index of the sequence command.
        /// </summary>
        /// <param name="commands">The command index.</param>
        /// <returns>The command index.</returns>
        public int Index(List<SequenceCommand> commands) => commands.IndexOf(this);

        /// <summary>
        /// Command parameters.
        /// </summary>
        public static Dictionary<SequenceCommands, SequenceCommandParameter> CommandParameters = new Dictionary<SequenceCommands, SequenceCommandParameter>() {
            { SequenceCommands.Note, SequenceCommandParameter.NoteParam },
            { SequenceCommands.Wait, SequenceCommandParameter.VariableLength },
            { SequenceCommands.ProgramChange, SequenceCommandParameter.VariableLength },
            { SequenceCommands.OpenTrack, SequenceCommandParameter.OpenTrack },
            { SequenceCommands.Jump, SequenceCommandParameter.U24 },
            { SequenceCommands.Call, SequenceCommandParameter.U24 },
            { SequenceCommands.Random, SequenceCommandParameter.Random },
            { SequenceCommands.Variable, SequenceCommandParameter.Variable },
            { SequenceCommands.If, SequenceCommandParameter.If },
            { SequenceCommands.Time, SequenceCommandParameter.Time },
            { SequenceCommands.TimeRandom, SequenceCommandParameter.TimeRandom },
            { SequenceCommands.TimeVariable, SequenceCommandParameter.TimeVariable },
            { SequenceCommands.Timebase, SequenceCommandParameter.U8 },
            { SequenceCommands.EnvHold, SequenceCommandParameter.U8 },
            { SequenceCommands.Monophonic, SequenceCommandParameter.Bool },
            { SequenceCommands.VelocityRange, SequenceCommandParameter.U8 },
            { SequenceCommands.BiquadType, SequenceCommandParameter.U8 },
            { SequenceCommands.BiquadValue, SequenceCommandParameter.U8 },
            { SequenceCommands.BankSelect, SequenceCommandParameter.U8 },
            { SequenceCommands.ModPhase, SequenceCommandParameter.U8 },
            { SequenceCommands.ModCurve, SequenceCommandParameter.U8 },
            { SequenceCommands.FrontBypass, SequenceCommandParameter.Bool },
            { SequenceCommands.Pan, SequenceCommandParameter.U8 },
            { SequenceCommands.Volume, SequenceCommandParameter.U8 },
            { SequenceCommands.MainVolume, SequenceCommandParameter.U8 },
            { SequenceCommands.Transpose, SequenceCommandParameter.S8 },
            { SequenceCommands.PitchBend, SequenceCommandParameter.S8 },
            { SequenceCommands.BendRange, SequenceCommandParameter.U8 },
            { SequenceCommands.Prio, SequenceCommandParameter.U8 },
            { SequenceCommands.NoteWait, SequenceCommandParameter.Bool },
            { SequenceCommands.Tie, SequenceCommandParameter.Bool },
            { SequenceCommands.Porta, SequenceCommandParameter.U8 },
            { SequenceCommands.ModDepth, SequenceCommandParameter.U8 },
            { SequenceCommands.ModSpeed, SequenceCommandParameter.U8 },
            { SequenceCommands.ModType, SequenceCommandParameter.U8 },
            { SequenceCommands.ModRange, SequenceCommandParameter.U8 },
            { SequenceCommands.PortaSw, SequenceCommandParameter.Bool },
            { SequenceCommands.PortaTime, SequenceCommandParameter.U8 },
            { SequenceCommands.Attack, SequenceCommandParameter.U8 },
            { SequenceCommands.Decay, SequenceCommandParameter.U8 },
            { SequenceCommands.Sustain, SequenceCommandParameter.U8 },
            { SequenceCommands.Release, SequenceCommandParameter.U8 },
            { SequenceCommands.LoopStart, SequenceCommandParameter.U8 },
            { SequenceCommands.Volume2, SequenceCommandParameter.U8 },
            { SequenceCommands.PrintVar, SequenceCommandParameter.U8 },
            { SequenceCommands.SurroundPan, SequenceCommandParameter.U8 },
            { SequenceCommands.LpfCutoff, SequenceCommandParameter.U8 },
            { SequenceCommands.FxSendA, SequenceCommandParameter.U8 },
            { SequenceCommands.FxSendB, SequenceCommandParameter.U8 },
            { SequenceCommands.MainSend, SequenceCommandParameter.U8 },
            { SequenceCommands.InitPan, SequenceCommandParameter.U8 },
            { SequenceCommands.Mute, SequenceCommandParameter.U8 },
            { SequenceCommands.FxSendC, SequenceCommandParameter.U8 },
            { SequenceCommands.Damper, SequenceCommandParameter.Bool },
            { SequenceCommands.ModDelay, SequenceCommandParameter.S16 },
            { SequenceCommands.Tempo, SequenceCommandParameter.S16 },
            { SequenceCommands.SweepPitch, SequenceCommandParameter.S16 },
            { SequenceCommands.ModPeriod, SequenceCommandParameter.S16 },
            { SequenceCommands.Extended, SequenceCommandParameter.Extended },
            { SequenceCommands.EnvReset, SequenceCommandParameter.None },
            { SequenceCommands.LoopEnd, SequenceCommandParameter.None },
            { SequenceCommands.Return, SequenceCommandParameter.None },
            { SequenceCommands.AllocateTrack, SequenceCommandParameter.U16 },
            { SequenceCommands.Fin, SequenceCommandParameter.None },
            { SequenceCommands.SetVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.AddVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.SubVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.MulVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.DivVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.ShiftVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.RandVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.AndVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.OrVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.XorVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.NotVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.ModVar, SequenceCommandParameter.U8S16 },
            { SequenceCommands.CmpEq, SequenceCommandParameter.U8S16 },
            { SequenceCommands.CmpGe, SequenceCommandParameter.U8S16 },
            { SequenceCommands.CmpGt, SequenceCommandParameter.U8S16 },
            { SequenceCommands.CmpLe, SequenceCommandParameter.U8S16 },
            { SequenceCommands.CmpLt, SequenceCommandParameter.U8S16 },
            { SequenceCommands.CmpNe, SequenceCommandParameter.U8S16 },
            { SequenceCommands.Mod2Curve, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod2Phase, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod2Depth, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod2Speed, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod2Type, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod2Range, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod2Delay, SequenceCommandParameter.S16 },
            { SequenceCommands.Mod2Period, SequenceCommandParameter.S16 },
            { SequenceCommands.Mod3Curve, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod3Phase, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod3Depth, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod3Speed, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod3Type, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod3Range, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod3Delay, SequenceCommandParameter.S16 },
            { SequenceCommands.Mod3Period, SequenceCommandParameter.S16 },
            { SequenceCommands.Mod4Curve, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod4Phase, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod4Depth, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod4Speed, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod4Type, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod4Range, SequenceCommandParameter.U8 },
            { SequenceCommands.Mod4Delay, SequenceCommandParameter.S16 },
            { SequenceCommands.Mod4Period, SequenceCommandParameter.S16 },
            { SequenceCommands.UserCall, SequenceCommandParameter.S16 }
        };

        /// <summary>
        /// Command strings.
        /// </summary>
        public static Dictionary<SequenceCommands, string> CommandStrings = new Dictionary<SequenceCommands, string>() {
            { SequenceCommands.Wait, "wait" },
            { SequenceCommands.ProgramChange, "prg" },
            { SequenceCommands.OpenTrack, "opentrack" },
            { SequenceCommands.Jump, "jump" },
            { SequenceCommands.Call, "call" },
            { SequenceCommands.Random, "_r" },
            { SequenceCommands.Variable, "_v" },
            { SequenceCommands.If, "_if" },
            { SequenceCommands.Time, "_t" },
            { SequenceCommands.TimeRandom, "_tr" },
            { SequenceCommands.TimeVariable, "_tv" },
            { SequenceCommands.Timebase, "timebase" },
            { SequenceCommands.EnvHold, "env_hold" },
            { SequenceCommands.Monophonic, "monophonic_" },
            { SequenceCommands.VelocityRange, "velocity_range" },
            { SequenceCommands.BiquadType, "biquad_type" },
            { SequenceCommands.BiquadValue, "biquad_value" },
            { SequenceCommands.BankSelect, "bank_select" },
            { SequenceCommands.ModPhase, "mod_phase" },
            { SequenceCommands.ModCurve, "mod_curve" },
            { SequenceCommands.FrontBypass, "front_bypass" },
            { SequenceCommands.Pan, "pan" },
            { SequenceCommands.Volume, "volume" },
            { SequenceCommands.MainVolume, "main_volume" },
            { SequenceCommands.Transpose, "transpose" },
            { SequenceCommands.PitchBend, "pitchbend" },
            { SequenceCommands.BendRange, "bendrange" },
            { SequenceCommands.Prio, "prio" },
            { SequenceCommands.NoteWait, "notewait" },
            { SequenceCommands.Tie, "tie" },
            { SequenceCommands.Porta, "porta" },
            { SequenceCommands.ModDepth, "mod_depth" },
            { SequenceCommands.ModSpeed, "mod_speed" },
            { SequenceCommands.ModType, "mod_type" },
            { SequenceCommands.ModRange, "mod_range" },
            { SequenceCommands.PortaSw, "porta" },
            { SequenceCommands.PortaTime, "porta_time" },
            { SequenceCommands.Attack, "attack" },
            { SequenceCommands.Decay, "decay" },
            { SequenceCommands.Sustain, "sustain" },
            { SequenceCommands.Release, "release" },
            { SequenceCommands.LoopStart, "loop_start" },
            { SequenceCommands.Volume2, "volume2" },
            { SequenceCommands.PrintVar, "printvar" },
            { SequenceCommands.SurroundPan, "span" },
            { SequenceCommands.LpfCutoff, "lpf_cutoff" },
            { SequenceCommands.FxSendA, "fxsend_a" },
            { SequenceCommands.FxSendB, "fxsend_b" },
            { SequenceCommands.MainSend, "mainsend" },
            { SequenceCommands.InitPan, "init_pan" },
            { SequenceCommands.Mute, "mute" },
            { SequenceCommands.FxSendC, "fxsend_c" },
            { SequenceCommands.Damper, "damper" },
            { SequenceCommands.ModDelay, "mod_delay" },
            { SequenceCommands.Tempo, "tempo" },
            { SequenceCommands.SweepPitch, "sweep_pitch" },
            { SequenceCommands.ModPeriod, "mod_period" },
            { SequenceCommands.EnvReset, "env_reset" },
            { SequenceCommands.LoopEnd, "loop_end" },
            { SequenceCommands.Return, "ret" },
            { SequenceCommands.AllocateTrack, "alloctrack" },
            { SequenceCommands.Fin, "fin" },
            { SequenceCommands.SetVar, "setvar" },
            { SequenceCommands.AddVar, "addvar" },
            { SequenceCommands.SubVar, "subvar" },
            { SequenceCommands.MulVar, "mulvar" },
            { SequenceCommands.DivVar, "divvar" },
            { SequenceCommands.ShiftVar, "shiftvar" },
            { SequenceCommands.RandVar, "randvar" },
            { SequenceCommands.AndVar, "andvar" },
            { SequenceCommands.OrVar, "orvar" },
            { SequenceCommands.XorVar, "xorvar" },
            { SequenceCommands.NotVar, "notvar" },
            { SequenceCommands.ModVar, "modvar" },
            { SequenceCommands.CmpEq, "cmp_eq" },
            { SequenceCommands.CmpGe, "cmp_ge" },
            { SequenceCommands.CmpGt, "cmp_gt" },
            { SequenceCommands.CmpLe, "cmp_le" },
            { SequenceCommands.CmpLt, "cmp_lt" },
            { SequenceCommands.CmpNe, "cmp_ne" },
            { SequenceCommands.Mod2Curve, "mod2_curve" },
            { SequenceCommands.Mod2Phase, "mod2_phase" },
            { SequenceCommands.Mod2Depth, "mod2_depth" },
            { SequenceCommands.Mod2Speed, "mod2_speed" },
            { SequenceCommands.Mod2Type, "mod2_type" },
            { SequenceCommands.Mod2Range, "mod2_range" },
            { SequenceCommands.Mod2Delay, "mod2_delay" },
            { SequenceCommands.Mod2Period, "mod2_period" },
            { SequenceCommands.Mod3Curve, "mod3_curve" },
            { SequenceCommands.Mod3Phase, "mod3_phase" },
            { SequenceCommands.Mod3Depth, "mod3_depth" },
            { SequenceCommands.Mod3Speed, "mod3_speed" },
            { SequenceCommands.Mod3Type, "mod3_type" },
            { SequenceCommands.Mod3Range, "mod3_range" },
            { SequenceCommands.Mod3Delay, "mod3_delay" },
            { SequenceCommands.Mod3Period, "mod3_period" },
            { SequenceCommands.Mod4Curve, "mod4_curve" },
            { SequenceCommands.Mod4Phase, "mod4_phase" },
            { SequenceCommands.Mod4Depth, "mod4_depth" },
            { SequenceCommands.Mod4Speed, "mod4_speed" },
            { SequenceCommands.Mod4Type, "mod4_type" },
            { SequenceCommands.Mod4Range, "mod4_range" },
            { SequenceCommands.Mod4Delay, "mod4_delay" },
            { SequenceCommands.Mod4Period, "mod4_period" },
            { SequenceCommands.UserCall, "userproc" }
        };

        /// <summary>
        /// Parameter mode.
        /// </summary>
        public enum ParameterMode { 
            Normal,
            Extended,
            NoParameter
        }

        /// <summary>
        /// Read the command.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="p">The platform.</param>
        /// <param name="parameterMode">Command read mode.</param>
        public void Read(FileReader r, SequencePlatform p, ParameterMode parameterMode = ParameterMode.Normal) {

            //Set byte order.
            r.ByteOrder = p.SequenceDataByteOrder();

            //Get the type.
            byte identifier = r.ReadByte();
            if (parameterMode != ParameterMode.Extended) {
                CommandType = p.CommandMap().FirstOrDefault(x => x.Value == identifier).Key;
            } else {
                CommandType = p.ExtendedCommands().FirstOrDefault(x => x.Value == identifier).Key;
            }

            //Note.
            if (identifier < 0x80) {
                CommandType = SequenceCommands.Note;
            }

            //Switch the parameter type.
            switch (CommandParameters[CommandType]) {

                //Note command.
                case SequenceCommandParameter n when (int)CommandType < 0x80:
                    if (parameterMode == ParameterMode.NoParameter) {
                        Parameter = new NoteParameter() { Note = (Notes)identifier, Velocity = r.ReadByte() };
                        return;
                    }
                    Parameter = new NoteParameter() { Note = (Notes)identifier, Velocity = r.ReadByte(), Length = VariableLength.ReadVariableLength(r, 4) };
                    break;

                //Open track.
                case SequenceCommandParameter.OpenTrack:
                    if (parameterMode == ParameterMode.NoParameter) {
                        Parameter = new OpenTrackParameter() { TrackNumber = r.ReadByte() };
                        return;
                    }
                    Parameter = new OpenTrackParameter() { TrackNumber = r.ReadByte(), Offset = r.Read<UInt24>() };
                    break;

                //Variable length.
                case SequenceCommandParameter.VariableLength:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    Parameter = VariableLength.ReadVariableLength(r, 4);
                    break;

                //U24.
                case SequenceCommandParameter.U24:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    Parameter = new UInt24Parameter() { Offset = r.Read<UInt24>() };
                    break;

                //Random.
                case SequenceCommandParameter.Random:
                    SequenceCommand rSeq = new SequenceCommand();
                    rSeq.Read(r, p, ParameterMode.NoParameter);
                    Parameter = new RandomParameter { Command = rSeq, Min = r.ReadInt16(), Max = r.ReadInt16() };
                    break;

                //Variable.
                case SequenceCommandParameter.Variable:
                    SequenceCommand vSeq = new SequenceCommand();
                    vSeq.Read(r, p, ParameterMode.NoParameter);
                    Parameter = new VariableParameter { Command = vSeq, Variable = r.ReadByte() };
                    break;

                //If.
                case SequenceCommandParameter.If:
                    SequenceCommand ifSeq = new SequenceCommand();
                    ifSeq.Read(r, p, ParameterMode.Normal);
                    Parameter = ifSeq;
                    break;

                //Time.
                case SequenceCommandParameter.Time:
                    SequenceCommand tSeq = new SequenceCommand();
                    tSeq.Read(r, p, ParameterMode.Normal);
                    Parameter = new TimeParameter() { Command = tSeq, Value = r.ReadInt16() };
                    break;

                //Time random.
                case SequenceCommandParameter.TimeRandom:
                    SequenceCommand trSeq = new SequenceCommand();
                    trSeq.Read(r, p, ParameterMode.Normal);
                    Parameter = new RandomParameter() { Command = trSeq, Min = r.ReadInt16(), Max = r.ReadInt16() };
                    break;

                //Time variable.
                case SequenceCommandParameter.TimeVariable:
                    SequenceCommand tvSeq = new SequenceCommand();
                    tvSeq.Read(r, p, ParameterMode.Normal);
                    Parameter = new VariableParameter() { Command = tvSeq, Variable = r.ReadByte() }; 
                    break;

                //U8.
                case SequenceCommandParameter.U8:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    Parameter = r.ReadByte();
                    break;

                //S8.
                case SequenceCommandParameter.S8:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    Parameter = r.ReadSByte();
                    break;

                //Bool.
                case SequenceCommandParameter.Bool:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    Parameter = r.ReadBoolean();
                    break;

                //U16.
                case SequenceCommandParameter.U16:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    Parameter = r.ReadUInt16();
                    break;

                //S16.
                case SequenceCommandParameter.S16:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    Parameter = r.ReadInt16();
                    break;

                //U8 S16.
                case SequenceCommandParameter.U8S16:
                    if (parameterMode == ParameterMode.NoParameter) {
                        Parameter = new U8S16Parameter() { U8 = r.ReadByte() };
                        return;
                    }
                    Parameter = new U8S16Parameter() { U8 = r.ReadByte(), S16 = r.ReadInt16() };
                    break;

                //Extended.
                case SequenceCommandParameter.Extended:
                    SequenceCommand seq = new SequenceCommand();
                    seq.Read(r, p, ParameterMode.Extended);
                    Parameter = seq.Parameter;
                    CommandType = seq.CommandType;
                    break;

            }

        }

        /// <summary>
        /// Write the command.
        /// </summary>
        /// <param name="w">The writer.</param>
        /// <param name="p">The platform.</param>
        /// <param name="parameterMode">Parameter mode.</param>
        public void Write(FileWriter w, SequencePlatform p, ParameterMode parameterMode = ParameterMode.Normal) {

            //Set endian.
            w.ByteOrder = p.SequenceDataByteOrder();

            //Write command type.
            if (parameterMode != ParameterMode.Extended) {
                if (CommandType == SequenceCommands.Note) {
                    w.Write((byte)(Parameter as NoteParameter).Note);
                } else {
                    if (p.ExtendedCommands().ContainsKey(CommandType)) {
                        w.Write(p.CommandMap()[SequenceCommands.Extended]);
                        w.Write(p.ExtendedCommands()[CommandType]);
                    } else {
                        w.Write(p.CommandMap()[CommandType]);
                    }
                }
            } else {
                w.Write(p.ExtendedCommands()[CommandType]);
            }

            //Write parameters.
            switch (CommandParameters[CommandType]) {

                //Note command.
                case SequenceCommandParameter n when (int)CommandType < 0x80:
                    if (parameterMode == ParameterMode.NoParameter) {
                        w.Write((Parameter as NoteParameter).Velocity);
                        return;
                    }
                    w.Write((Parameter as NoteParameter).Velocity);
                    VariableLength.WriteVariableLength(w, (Parameter as NoteParameter).Length);
                    break;

                //Open track.
                case SequenceCommandParameter.OpenTrack:
                    if (parameterMode == ParameterMode.NoParameter) {
                        w.Write((Parameter as OpenTrackParameter).TrackNumber);
                        return;
                    }
                    w.Write((Parameter as OpenTrackParameter).TrackNumber);
                    w.Write((Parameter as OpenTrackParameter).Offset);
                    break;

                //Variable length.
                case SequenceCommandParameter.VariableLength:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    VariableLength.WriteVariableLength(w, (uint)Parameter);
                    break;

                //U24.
                case SequenceCommandParameter.U24:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    w.Write((Parameter as UInt24Parameter).Offset);
                    break;

                //Random.
                case SequenceCommandParameter.Random:
                    (Parameter as RandomParameter).Command.Write(w, p, ParameterMode.NoParameter);
                    w.Write((Parameter as RandomParameter).Min);
                    w.Write((Parameter as RandomParameter).Max);
                    break;

                //Variable.
                case SequenceCommandParameter.Variable:
                    (Parameter as VariableParameter).Command.Write(w, p, ParameterMode.NoParameter);
                    w.Write((Parameter as VariableParameter).Variable);
                    break;

                //If.
                case SequenceCommandParameter.If:
                    (Parameter as SequenceCommand).Write(w, p, ParameterMode.Normal);
                    break;

                //Time.
                case SequenceCommandParameter.Time:
                    (Parameter as TimeParameter).Command.Write(w, p, ParameterMode.Normal);
                    w.Write((Parameter as TimeParameter).Value);
                    break;

                //Time random.
                case SequenceCommandParameter.TimeRandom:
                    (Parameter as RandomParameter).Command.Write(w, p, ParameterMode.Normal);
                    w.Write((Parameter as RandomParameter).Min);
                    w.Write((Parameter as RandomParameter).Max);
                    break;

                //Time variable.
                case SequenceCommandParameter.TimeVariable:
                    (Parameter as VariableParameter).Command.Write(w, p, ParameterMode.Normal);
                    w.Write((Parameter as VariableParameter).Variable);
                    break;

                //U8.
                case SequenceCommandParameter.U8:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    w.Write((byte)Parameter);
                    break;

                //S8.
                case SequenceCommandParameter.S8:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    w.Write((sbyte)Parameter);
                    break;

                //Bool.
                case SequenceCommandParameter.Bool:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    w.Write((bool)Parameter);
                    break;

                //U16.
                case SequenceCommandParameter.U16:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    w.Write((ushort)Parameter);
                    break;

                //S16.
                case SequenceCommandParameter.S16:
                    if (parameterMode == ParameterMode.NoParameter) {
                        return;
                    }
                    w.Write((short)Parameter);
                    break;

                //U8 S16.
                case SequenceCommandParameter.U8S16:
                    if (parameterMode == ParameterMode.NoParameter) {
                        w.Write((Parameter as U8S16Parameter).U8);
                        return;
                    }
                    w.Write((Parameter as U8S16Parameter).U8);
                    w.Write((Parameter as U8S16Parameter).S16);
                    break;

                //Extended.
                case SequenceCommandParameter.Extended:
                    (Parameter as SequenceCommand).Write(w, p, ParameterMode.Extended);
                    break;

            }

        }

        /// <summary>
        /// Convert the command to a string.
        /// </summary>
        /// <returns>The command as a string.</returns>
        public override string ToString() {
            var t = ToString(false);
            string ret = t.Item1;
            for (int i = 0; i < t.Item2.Count; i++) { 
                if (i != 0) { ret += ","; }
                ret += " ";
                ret += t.Item2[i];
            }
            return ret;
        }

        /// <summary>
        /// Convert the command to a string.
        /// </summary>
        /// <param name="noParameters">If there are no parameters.</param>
        /// <returns>The command as a string.</returns>
        public Tuple<string, List<string>> ToString(bool noParameters) {

            //Command string.
            string command = "";
            if ((int)CommandType < 0x80) {
                command = ((Notes)(Parameter as NoteParameter).Note).ToString();
            } else {
                command = CommandStrings[CommandType];
                if (command.StartsWith("_")) { command = ""; }
            }

            //Data string.
            List<string> data = new List<string>();

            //Get parameters.
            switch (CommandParameters[CommandType]) {

                //Note command.
                case SequenceCommandParameter n when (int)CommandType < 0x80:
                    data.Add((Parameter as NoteParameter).Velocity.ToString());
                    if (!noParameters) {
                        data.Add((Parameter as NoteParameter).Length.ToString());
                    }
                    break;

                //Open track.
                case SequenceCommandParameter.OpenTrack:
                    data.Add((Parameter as OpenTrackParameter).TrackNumber.ToString());
                    if (!noParameters) {
                        data.Add((Parameter as OpenTrackParameter).Label);
                    }
                    break;

                //Variable length.
                case SequenceCommandParameter.VariableLength:
                    if (!noParameters) {
                        data.Add(Parameter.ToString());
                    }
                    break;

                //U24.
                case SequenceCommandParameter.U24:
                    if (!noParameters) {
                        data.Add((Parameter as UInt24Parameter).Label.ToString());
                    }
                    break;

                //Random.
                case SequenceCommandParameter.Random:
                    var rTup = (Parameter as RandomParameter).Command.ToString(true);
                    command += rTup.Item1 + "_r";
                    data.AddRange(rTup.Item2);
                    data.Add((Parameter as RandomParameter).Min.ToString());
                    data.Add((Parameter as RandomParameter).Max.ToString());
                    break;

                //Variable.
                case SequenceCommandParameter.Variable:
                    var vTup = (Parameter as VariableParameter).Command.ToString(true);
                    command += vTup.Item1 + "_v";
                    data.AddRange(vTup.Item2);
                    data.Add((Parameter as VariableParameter).Variable.ToString());
                    break;

                //If.
                case SequenceCommandParameter.If:
                    var iTup = (Parameter as SequenceCommand).ToString(false);
                    command += iTup.Item1 + "_if";
                    data.AddRange(iTup.Item2);
                    break;

                //Time.
                case SequenceCommandParameter.Time:
                    var tTup = (Parameter as TimeParameter).Command.ToString(false);
                    command += tTup.Item1 + "_t";
                    data.AddRange(tTup.Item2);
                    data.Add((Parameter as TimeParameter).Value.ToString());
                    break;

                //Time random.
                case SequenceCommandParameter.TimeRandom:
                    var trTup = (Parameter as RandomParameter).Command.ToString(false);
                    command += trTup.Item1 + "_tr";
                    data.AddRange(trTup.Item2);
                    data.Add((Parameter as RandomParameter).Min.ToString());
                    data.Add((Parameter as RandomParameter).Max.ToString());
                    break;

                //Time variable.
                case SequenceCommandParameter.TimeVariable:
                    var tvTup = (Parameter as VariableParameter).Command.ToString(false);
                    command += tvTup.Item1 + "_tv";
                    data.AddRange(tvTup.Item2);
                    data.Add((Parameter as VariableParameter).Variable.ToString());
                    break;

                //U8.
                case SequenceCommandParameter.U8:
                    if (!noParameters) {
                        data.Add(Parameter.ToString());
                    }
                    break;

                //U8.
                case SequenceCommandParameter.S8:
                    if (!noParameters) {
                        data.Add(Parameter.ToString());
                    }
                    break;

                //Bool.
                case SequenceCommandParameter.Bool:
                    if (!noParameters) {
                        command += (CommandType == SequenceCommands.Tie ? "" : "_") + (((bool)Parameter) ? "on" : "off");
                    }
                    break;

                //U16.
                case SequenceCommandParameter.U16:
                    if (!noParameters) {
                        data.Add(Parameter.ToString());
                    }
                    break;

                //S16.
                case SequenceCommandParameter.S16:
                    if (!noParameters) {
                        data.Add(Parameter.ToString());
                    }
                    break;

                //U8 S16.
                case SequenceCommandParameter.U8S16:
                    data.Add((Parameter as U8S16Parameter).U8.ToString());
                    if (!noParameters) {
                        data.Add((Parameter as U8S16Parameter).S16.ToString());
                    }
                    break;

                //Extended.
                case SequenceCommandParameter.Extended:
                    return (Parameter as SequenceCommand).ToString(false);

            }

            //Return the final product.
            return new Tuple<string, List<string>>(command, data);

        }

        /// <summary>
        /// Read a command from a string.
        /// </summary>
        /// <param name="s">The command string.</param>
        /// <param name="p">The sequence platform.</param>
        /// <param name="publicLabels">Public labels.</param>
        /// <param name="privateLabels">Private labels.</param>
        public void FromString(string s, SequencePlatform p, Dictionary<string, int> publicLabels, Dictionary<string, int> privateLabels) {

            //Get base command.
            SequenceCommands b = SequenceCommands.Note;

            //Command string.
            string c = s;
            string cT = c;
            cT = cT.Replace("_on", "").Replace("_off", "");
            cT = cT.Replace("on", "").Replace("off", "");
            try { c = c.Substring(0, c.IndexOf(' ')); } catch { }
            try { cT = cT.Substring(0, cT.IndexOf(' ')); } catch { }
            if (cT.EndsWith("_if")) { cT = cT.Replace("_if", ""); }
            if (cT.EndsWith("_tv")) { cT = cT.Replace("_tv", ""); }
            if (cT.EndsWith("_tr")) { cT = cT.Replace("_tr", ""); }
            if (cT.EndsWith("_t")) { cT = cT.Replace("_t", ""); }
            if (cT.EndsWith("_v")) { cT = cT.Replace("_v", ""); }
            if (cT.EndsWith("_r")) { cT = cT.Replace("_r", ""); }

            //Get command type.
            foreach (var e in CommandStrings) {
                if (!e.Value.StartsWith("_") && cT.Equals(e.Value)) {
                    b = e.Key;
                    if (c.Contains("porta") && !c.Contains("porta_on") && !c.Contains("porta_off")) { b = SequenceCommands.Porta; }
                    if (c.Contains("porta_time")) { b = SequenceCommands.PortaTime; }
                }
            }

            //Get data.
            string dataString = "";
            try { dataString = s.Substring(s.IndexOf(' ')); } catch { }
            dataString = dataString.Replace(" ", "");
            string[] data = dataString.Split(',');

            //Flags.
            bool isIf = false;
            bool isTimeVariable = false;
            bool isTimeRandom = false;
            bool isTime = false;
            bool isVariable = false;
            bool isRandom = false;

            //Footer flags.
            if (c.Contains("_if_") || c.Contains("_if ") || c.EndsWith("_if")) {
                isIf = true;
            }

            //Middle flags.
            if (c.Contains("_tv_") || c.Contains("_tv ") || c.EndsWith("_tv")) {
                isTimeVariable = true;
            } else if (c.Contains("_tr_") || c.Contains("_tr ") || c.EndsWith("_tr")) {
                isTimeRandom = true;
            } else if (c.Contains("_t_") || c.Contains("_t ") || c.EndsWith("_t")) {
                isTime = true;
            }

            //Header flags.
            if (c.Contains("_v_") || c.Contains("_v ") || c.EndsWith("_v")) {
                isVariable = true;
            } else if (c.Contains("_r_") || c.Contains("_r ") || c.EndsWith("_r")) {
                isRandom = true;
            }

            //Bool data.
            bool boolParam = false;
            if (c.Contains("on")) {
                boolParam = true;
            }

            //Actually start building the command.
            CommandType = b;

            //Data ptr.
            int dataPtr = 0;

            //Don't read last parameter.
            bool noLastParameter = isRandom || isVariable;

            //Read data.
            switch (CommandParameters[b]) {
                case SequenceCommandParameter.Bool:
                    Parameter = boolParam;
                    break;
                case SequenceCommandParameter.NoteParam:
                    string note = c;
                    try { note = note.Substring(0, note.IndexOf("_")); } catch { }
                    if (!noLastParameter) {
                        Parameter = new NoteParameter() { Note = (Notes)Enum.Parse(typeof(Notes), note), Velocity = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels), Length = (uint)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                    } else {
                        Parameter = new NoteParameter() { Note = (Notes)Enum.Parse(typeof(Notes), note), Velocity = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                    }
                    break;
                case SequenceCommandParameter.OpenTrack:
                    if (!noLastParameter) {
                        Parameter = new OpenTrackParameter() { TrackNumber = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels), m_Index = (int)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                    } else {
                        Parameter = new OpenTrackParameter() { TrackNumber = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                    }
                    break;
                case SequenceCommandParameter.S16:
                    if (!noLastParameter) {
                        Parameter = (short)ParseData(data[dataPtr++], publicLabels, privateLabels);
                    }
                    break;
                case SequenceCommandParameter.U16:
                    if (!noLastParameter) {
                        Parameter = (ushort)ParseData(data[dataPtr++], publicLabels, privateLabels);
                    }
                    break;
                case SequenceCommandParameter.U24:
                    if (!noLastParameter) {
                        Parameter = new UInt24Parameter() { m_Index = (int)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                    }
                    break;
                case SequenceCommandParameter.U8:
                    if (!noLastParameter) {
                        Parameter = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels);
                    }
                    break;
                case SequenceCommandParameter.S8:
                    if (!noLastParameter) {
                        Parameter = (sbyte)ParseData(data[dataPtr++], publicLabels, privateLabels);
                    }
                    break;
                case SequenceCommandParameter.U8S16:
                    if (!noLastParameter) {
                        Parameter = new U8S16Parameter() { U8 = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels), S16 = (short)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                    } else {
                        Parameter = new U8S16Parameter() { U8 = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                    }
                    break;
                case SequenceCommandParameter.VariableLength:
                    if (!noLastParameter) {
                        Parameter = (uint)ParseData(data[dataPtr++], publicLabels, privateLabels);
                    }
                    break;
            }

            //Extended command.
            if (p.ExtendedCommands().ContainsKey(b)) {
                Parameter = Duplicate();
                CommandType = SequenceCommands.Extended;
            }

            //Header flags.
            if (isRandom) {
                Parameter = new RandomParameter() { Command = Duplicate(), Min = (short)ParseData(data[dataPtr++], publicLabels, privateLabels), Max = (short)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                CommandType = SequenceCommands.Random;
            } else if (isVariable) {
                Parameter = new VariableParameter() { Command = Duplicate(), Variable = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                CommandType = SequenceCommands.Variable;
            }

            //Middle flags.
            if (isTime) {
                Parameter = new TimeParameter() { Command = Duplicate(), Value = (short)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                CommandType = SequenceCommands.Time;
            } else if (isTimeRandom) {
                Parameter = new RandomParameter() { Command = Duplicate(), Min = (short)ParseData(data[dataPtr++], publicLabels, privateLabels), Max = (short)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                CommandType = SequenceCommands.TimeRandom;
            } else if (isTimeVariable) {
                Parameter = new VariableParameter() { Command = Duplicate(), Variable = (byte)ParseData(data[dataPtr++], publicLabels, privateLabels) };
                CommandType = SequenceCommands.TimeVariable;
            }

            //Footer flags.
            if (isIf) {
                Parameter = Duplicate();
                CommandType = SequenceCommands.If;
            }

        }

        /// <summary>
        /// Parse data.
        /// </summary>
        /// <param name="data">The data string.</param>
        /// <param name="publicLabels">The public labels.</param>
        /// <param name="privateLabels">The private labels.</param>
        /// <returns>The data.</returns>
        private long ParseData(string data, Dictionary<string, int> publicLabels, Dictionary<string, int> privateLabels) {

            //Labels.
            if (publicLabels.ContainsKey(data)) {
                return publicLabels[data];
            }
            if (privateLabels.ContainsKey(data)) {
                return privateLabels[data];
            }

            //Just parse it as a number.
            if (data.StartsWith("0x")) {
                return Convert.ToInt64(data.Substring(2), 16);
            } else if (data.StartsWith("0o")) { 
                return Convert.ToInt64(data.Substring(2), 8);
            }else if (data.StartsWith("0b")) {
                return Convert.ToInt64(data.Substring(2), 2);
            } else {
                return long.Parse(data);
            }

        }

        /// <summary>
        /// Duplicate the command.
        /// </summary>
        /// <returns>A duplicate command.</returns>
        public SequenceCommand Duplicate() {
            SequenceCommand seq = new SequenceCommand();
            seq.CommandType = CommandType;
            switch (CommandParameters[seq.CommandType]) {
                case SequenceCommandParameter.Bool:
                    try { seq.Parameter = (bool)Parameter; } catch { }
                    break;
                case SequenceCommandParameter.Extended:
                    seq.Parameter = (Parameter as SequenceCommand).Duplicate();
                    break;
                case SequenceCommandParameter.If:
                    seq.Parameter = (Parameter as SequenceCommand).Duplicate();
                    break;
                case SequenceCommandParameter.NoteParam:
                    try { seq.Parameter = new NoteParameter() { Note = (Parameter as NoteParameter).Note, Length = (Parameter as NoteParameter).Length, Velocity = (Parameter as NoteParameter).Velocity }; } catch { seq.Parameter = new NoteParameter() { Note = (Parameter as NoteParameter).Note, Velocity = (Parameter as NoteParameter).Velocity }; }
                    break;
                case SequenceCommandParameter.OpenTrack:
                    try { seq.Parameter = new OpenTrackParameter() { m_Index = (Parameter as OpenTrackParameter).m_Index, ReferenceCommand = (Parameter as OpenTrackParameter).ReferenceCommand, Label = (Parameter as OpenTrackParameter).Label, Offset = (Parameter as OpenTrackParameter).Offset, TrackNumber = (Parameter as OpenTrackParameter).TrackNumber }; } catch { seq.Parameter = new OpenTrackParameter() { TrackNumber = (Parameter as OpenTrackParameter).TrackNumber }; }
                    break;
                case SequenceCommandParameter.Random:
                    seq.Parameter = new RandomParameter() { Command = (Parameter as RandomParameter).Command.Duplicate(), Min = (Parameter as RandomParameter).Min, Max = (Parameter as RandomParameter).Max };
                    break;
                case SequenceCommandParameter.S16:
                    try { seq.Parameter = (short)Parameter; } catch { }
                    break;
                case SequenceCommandParameter.Time:
                    seq.Parameter = new TimeParameter() { Command = (Parameter as TimeParameter).Command.Duplicate(), Value = (Parameter as TimeParameter).Value };
                    break;
                case SequenceCommandParameter.TimeRandom:
                    seq.Parameter = new RandomParameter() { Command = (Parameter as RandomParameter).Command.Duplicate(), Min = (Parameter as RandomParameter).Min, Max = (Parameter as RandomParameter).Max };
                    break;
                case SequenceCommandParameter.TimeVariable:
                    seq.Parameter = new VariableParameter() { Command = (Parameter as VariableParameter).Command.Duplicate(), Variable = (Parameter as VariableParameter).Variable };
                    break;
                case SequenceCommandParameter.U16:
                    try { seq.Parameter = (ushort)Parameter; } catch { }
                    break;
                case SequenceCommandParameter.U24:
                    try { seq.Parameter = new UInt24Parameter() { m_Index = (Parameter as UInt24Parameter).m_Index, ReferenceCommand = (Parameter as UInt24Parameter).ReferenceCommand, Label = (Parameter as UInt24Parameter).Label, Offset = (Parameter as UInt24Parameter).Offset }; } catch { }
                    break;
                case SequenceCommandParameter.U8:
                    try { seq.Parameter = (byte)Parameter; } catch { }
                    break;
                case SequenceCommandParameter.S8:
                    try { seq.Parameter = (sbyte)Parameter; } catch { }
                    break;
                case SequenceCommandParameter.U8S16:
                    try { seq.Parameter = new U8S16Parameter() { U8 = (Parameter as U8S16Parameter).U8, S16 = (Parameter as U8S16Parameter).S16 }; } catch { seq.Parameter = new U8S16Parameter() { U8 = (Parameter as U8S16Parameter).U8}; }
                    break;
                case SequenceCommandParameter.Variable:
                    seq.Parameter = new VariableParameter() { Command = (Parameter as VariableParameter).Command.Duplicate(), Variable = (Parameter as VariableParameter).Variable };
                    break;
                case SequenceCommandParameter.VariableLength:
                    try { seq.Parameter = (uint)Parameter; } catch { }
                    break;
            }
            return seq;
        }

    }

}
