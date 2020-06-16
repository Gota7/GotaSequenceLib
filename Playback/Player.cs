using GotaSoundIO.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace GotaSequenceLib.Playback {

    /// <summary>
    /// A player.
    /// </summary>
    public class Player : IDisposable {

        /// <summary>
        /// Clock speed.
        /// </summary>
        public uint ClockSpeed = 16756991;

        /// <summary>
        /// Sequence variables.
        /// </summary>
        public short[] Vars = new short[0x20];

        /// <summary>
        /// Banks.
        /// </summary>
        public PlayableBank[] Banks;

        /// <summary>
        /// Wave archives to use.
        /// </summary>
        public RiffWave[][] WaveArchives;

        /// <summary>
        /// Volume.
        /// </summary>
        public byte Volume = 127;

        //Private variables.
        private readonly Track[] _tracks = new Track[0x10];
        private readonly Mixer _mixer;
        private readonly TimeBarrier _time;
        private Thread _thread;
        private int _randSeed;
        private Random _rand;
        private ushort _tempo;
        private int _tempoStack;
        private long _elapsedLoops;
        private int currEventOverride;

        public List<SequenceCommand> Events { get; private set; }
        public Dictionary<int, int> Ticks { get; private set; }
        public long ElapsedTicks { get; private set; }
        public long MaxTicks { get; private set; }
        public bool ShouldFadeOut { get; set; } = true;
        public bool DontFadeSong { get; set; }
        public long NumLoops { get; set; } = 0;
        private int _longestTrack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;
        public event NotePressedHandler NotePressed = delegate {};
        public event NotePressedHandler NoteReleased = delegate {};
        public delegate void NotePressedHandler(object sender, NoteEventArgs e);

        /// <summary>
        /// Note event args.
        /// </summary>
        public class NoteEventArgs : EventArgs {
            public int TrackId;
            public Notes Note;
            public bool On;
        }

        /// <summary>
        /// Create a new player.
        /// </summary>
        /// <param name="mixer">The mixer.</param>
        public Player(Mixer mixer) {

            //Set up stuff.
            for (byte i = 0; i < 0x10; i++) {
                _tracks[i] = new Track(i, this);
            }
            _mixer = mixer;
            _time = new TimeBarrier(192);

        }

        /// <summary>
        /// Prepare for a song.
        /// </summary>
        /// <param name="banks">The banks.</param>
        /// <param name="waveArchives">The wave archives.</param>
        public void PrepareForSong(PlayableBank[] banks, RiffWave[][] waveArchives) {
            Banks = banks;
            WaveArchives = waveArchives;
        }

        /// <summary>
        /// Load a song.
        /// </summary>
        /// <param name="commands">The sequence commands.</param>
        /// <param name="startOffset">Start offset.</param>
        public void LoadSong(List<SequenceCommand> commands, int startOffset = 0) {
            Stop();
            Events = commands;
            _randSeed = new Random().Next();
            currEventOverride = startOffset;
            InitEmulation();
            SetTicks();
            currEventOverride = startOffset;
        }

        /// <summary>
        /// Create a thread.
        /// </summary>
        private void CreateThread() {
            _thread = new Thread(Tick);
            _thread.Start();
        }

        /// <summary>
        /// Wait thread.
        /// </summary>
        private void WaitThread() {
            if (_thread != null && (_thread.ThreadState == ThreadState.Running || _thread.ThreadState == ThreadState.WaitSleepJoin)) {
                _thread.Join();
            }
        }

        /// <summary>
        /// Initialize emulation.
        /// </summary>
        private void InitEmulation() {

            //Defaults.
            _tempo = 120; // Confirmed: default tempo is 120 (MKDS 75)
            _tempoStack = 0;
            _elapsedLoops = 0;
            ElapsedTicks = 0;
            _mixer.ResetFade();
            _rand = new Random(_randSeed);
            for (int i = 0; i < 0x10; i++) {
                _tracks[i].Init();
            }

            //Initialize player and global variables. Global variables should not have an effect in this program.
            for (int i = 0; i < 0x20; i++) {
                Vars[i] = -1;
            }

        }

        /// <summary>
        /// Play a note.
        /// </summary>
        /// <param name="track">The track to play a note on.</param>
        /// <param name="key">The note key.</param>
        /// <param name="velocity">The note velocity.</param>
        /// <param name="duration">The note duration.</param>
        public void PlayNote(Track track, byte key, byte velocity, int duration) {
            Channel channel = null;
            NotePressed(this, new NoteEventArgs() { TrackId = _tracks.ToList().IndexOf(track), Note = (Notes)key, On = true });
            track.NoteDown = true;
            if (track.Tie && track.Channels.Count != 0) {
                channel = track.Channels.Last();
                channel.Key = key;
                channel.NoteVelocity = velocity;
            } else {
                NotePlayBackInfo param = Banks[track.BankNum].GetNotePlayBackInfo(track.Voice, (Notes)key, velocity);
                if (param != null) {
                    InstrumentType type = param.InstrumentType;
                    channel = _mixer.AllocateChannel(type, track);
                    if (channel != null) {
                        if (track.Tie) {
                            duration = -1;
                        }
                        byte release = param.Release;
                        if (release == 0xFF) {
                            duration = -1;
                            release = 0;
                        }
                        bool started = false;
                        switch (type) {
                            case InstrumentType.PCM: {
                                RiffWave wave = null;
                                try { wave = WaveArchives[param.WarId][param.WaveId]; } catch { Console.WriteLine("Can't find wave specified by bank!"); }
                                if (wave != null) {
                                    channel.StartPCM(wave, duration, ClockSpeed);
                                    started = true;
                                }
                                break;
                            }
                            case InstrumentType.PSG: {
                                channel.StartPSG((byte)param.WaveId, duration);
                                started = true;
                                break;
                            }
                            case InstrumentType.Noise: {
                                channel.StartNoise(duration);
                                started = true;
                                break;
                            }
                        }
                        channel.Stop();
                        if (started) {
                            channel.Key = key;
                            byte baseKey = param.BaseKey;
                            channel.BaseKey = type != InstrumentType.PCM && baseKey == 0x7F ? (byte)60 : baseKey;
                            channel.NoteVelocity = velocity;
                            channel.SetAttack(param.Attack);
                            channel.SetDecay(param.Decay);
                            channel.SetSustain(param.Sustain);
                            channel.SetHold(param.Hold);
                            channel.SetRelease(release);
                            channel.StartingPan = (sbyte)(param.Pan - 0x40);
                            channel.Owner = track;
                            track.Channels.Add(channel);
                        } else {
                            return;
                        }
                    }
                }
            }
            if (channel != null) {
                if (track.Attack != 0xFF) {
                    channel.SetAttack(track.Attack);
                }
                if (track.Decay != 0xFF) {
                    channel.SetDecay(track.Decay);
                }
                if (track.Sustain != 0xFF) {
                    channel.SetSustain(track.Sustain);
                }
                if (track.Hold != 0xFF) {
                    channel.SetHold(track.Hold);
                }
                if (track.Release != 0xFF) {
                    channel.SetRelease(track.Release);
                }
                channel.SweepPitch = track.SweepPitch;
                if (track.Portamento) {
                    channel.SweepPitch += (short)((track.PortamentoKey - key) << 6); // "<< 6" is "* 0x40"
                }
                if (track.PortamentoTime != 0) {
                    channel.SweepLength = (track.PortamentoTime * track.PortamentoTime * Math.Abs(channel.SweepPitch)) >> 11; // ">> 11" is "/ 0x800"
                    channel.AutoSweep = true;
                } else {
                    channel.SweepLength = duration;
                    channel.AutoSweep = false;
                }
                channel.SweepCounter = 0;
            }
        }

        /// <summary>
        /// Get a variable value.
        /// </summary>
        /// <param name="varNum">The variable number.</param>
        /// <param name="trackNum">The track number.</param>
        /// <returns>The variable value.</returns>
        public short GetVar(int varNum, int trackNum) {
            if (varNum < 0x20) {
                return Vars[varNum];
            } else {
                return _tracks[trackNum].Vars[varNum - 0x20];
            }
        }

        /// <summary>
        /// Set a variable value.
        /// </summary>
        /// <param name="varNum">The variable number.</param>
        /// <param name="trackNum">The track number.</param>
        /// <param name="val">The variable value.</param>
        public void SetVar(int varNum, int trackNum, short val) {
            if (varNum < 0x20) {
                Vars[varNum] = val;
            } else {
                _tracks[trackNum].Vars[varNum - 0x20] = val;
            }
        }

        /// <summary>
        /// Tick.
        /// </summary>
        private void Tick() {
            _time.Start();
            while (true) {
                PlayerState state = State;
                bool playing = state == PlayerState.Playing;
                bool recording = state == PlayerState.Recording;
                if (!playing && !recording) {
                    _time.Stop();
                    return;
                }

                void MixerProcess() {
                    _mixer.ChannelTick();
                    _mixer.Process(playing, recording);
                }

                while (_tempoStack >= 240) {
                    _tempoStack -= 240;
                    bool allDone = true;
                    for (int i = 0; i < 0x10; i++) {
                        Track track = _tracks[i];
                        if (track.Enabled) {
                            track.Tick();
                            if (track.NoteDown && (track.Channels.Count == 0 || track.Channels.Last().State == EnvelopeState.Release)) {
                                track.NoteDown = false;
                                NoteReleased(this, new NoteEventArgs() { On = false, TrackId = i });
                            }
                            while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped) {
                                ExecuteNext(i);
                            }
                            if (i == _longestTrack) {
                                if (ElapsedTicks >= MaxTicks) {
                                    if (!track.Stopped) {
                                        long[] t = Events[track.CurEvent].Ticks;
                                        ElapsedTicks = t.Length == 0 ? 0 : t[_longestTrack] - track.Rest; // Prevent crashes with songs that don't load all ticks yet (See SetTicks())
                                        _elapsedLoops++;
                                        if (ShouldFadeOut && !_mixer.IsFading() && _elapsedLoops > NumLoops) {
                                            _mixer.BeginFadeOut();
                                        }
                                    }
                                } else {
                                    ElapsedTicks++;
                                }
                            }
                            if (!track.Stopped || track.Channels.Count != 0) {
                                allDone = false;
                            }
                        }
                    }
                    if (_mixer.IsFadeDone()) {
                        allDone = true;
                    }
                    if (allDone) {
                        MixerProcess();
                        State = PlayerState.Stopped;
                        SongEnded?.Invoke();
                        _time.Stop();
                        return;
                    }
                }
                _tempoStack += _tempo;
                MixerProcess();
                if (playing) {
                    _time.Wait();
                }
            }

            _time.Stop();
        }

        /// <summary>
        /// Get the command parameters.
        /// </summary>
        /// <param name="c">The command.</param>
        /// <param name="argumentNum">The argument number.</param>
        /// <param name="_rand">Random.</param>
        /// <param name="events">Events.</param>
        /// <returns>The parameter.</returns>
        public static int GetCommandParameter(SequenceCommand c, int argumentNum, Random _rand, List<SequenceCommand> events) {

            //Switch the command type.
            switch (SequenceCommand.CommandParameters[c.CommandType]) {

                case SequenceCommandParameter.Bool:
                    return ((bool)c.Parameter ? 1 : 0);

                case SequenceCommandParameter.None:
                    return 0;

                case SequenceCommandParameter.NoteParam:
                    switch (argumentNum) {
                        case 0:
                            return (int)(c.Parameter as NoteParameter).Note;
                        case 1:
                            return (c.Parameter as NoteParameter).Velocity;
                        case 2:
                            return (int)(c.Parameter as NoteParameter).Length;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                case SequenceCommandParameter.OpenTrack:
                    switch (argumentNum) {
                        case 0:
                            return (c.Parameter as OpenTrackParameter).TrackNumber;
                        case 1:
                            return (int)(c.Parameter as OpenTrackParameter).Index(events);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                case SequenceCommandParameter.Random:
                    int argsNumR = NumArguments(c);
                    if (argsNumR == argumentNum + 1) {
                        return _rand.Next((c.Parameter as RandomParameter).Min, (c.Parameter as RandomParameter).Max);
                    } else {
                        return GetCommandParameter((c.Parameter as RandomParameter).Command, argumentNum, _rand, events);
                    }

                case SequenceCommandParameter.S16:
                    return (short)c.Parameter;

                case SequenceCommandParameter.Time:
                    int argsNumT = NumArguments(c);
                    if (argsNumT == argumentNum + 1) {
                        return (c.Parameter as TimeParameter).Value;
                    } else {
                        return GetCommandParameter((c.Parameter as TimeParameter).Command, argumentNum, _rand, events);
                    }

                case SequenceCommandParameter.TimeRandom:
                    int argsNumTR = NumArguments(c);
                    if (argsNumTR == argumentNum + 1) {
                        return _rand.Next((c.Parameter as RandomParameter).Min, (c.Parameter as RandomParameter).Max);
                    } else {
                        return GetCommandParameter((c.Parameter as RandomParameter).Command, argumentNum, _rand, events);
                    }

                case SequenceCommandParameter.TimeVariable:
                    int argsNumTV = NumArguments(c);
                    if (argsNumTV == argumentNum + 1) {
                        return (c.Parameter as VariableParameter).Variable;
                    } else {
                        return GetCommandParameter((c.Parameter as VariableParameter).Command, argumentNum, _rand, events);
                    }

                case SequenceCommandParameter.U16:
                    return (ushort)c.Parameter;

                case SequenceCommandParameter.U24:
                    return (c.Parameter as UInt24Parameter).Index(events);

                case SequenceCommandParameter.U8:
                    return (byte)c.Parameter;

                case SequenceCommandParameter.S8:
                    return (sbyte)c.Parameter;

                case SequenceCommandParameter.U8S16:
                    switch (argumentNum) {
                        case 0:
                            return (c.Parameter as U8S16Parameter).U8;
                        case 1:
                            return (c.Parameter as U8S16Parameter).S16;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                case SequenceCommandParameter.Variable:
                    int argsNumV = NumArguments(c);
                    if (argsNumV == argumentNum + 1) {
                        return (c.Parameter as VariableParameter).Variable;
                    } else {
                        return GetCommandParameter((c.Parameter as VariableParameter).Command, argumentNum, _rand, events);
                    }

                case SequenceCommandParameter.VariableLength:
                    return (int)((uint)c.Parameter);

                case SequenceCommandParameter.If:
                    return GetCommandParameter((c.Parameter as SequenceCommand), argumentNum, _rand, events);

            }

            return 0;

        }

        /// <summary>
        /// Get the number of arguments.
        /// </summary>
        /// <param name="c">The sequence command.</param>
        /// <returns>The number of arguments.</returns>
        public static int NumArguments(SequenceCommand c) {

            //Switch the command type.
            switch (SequenceCommand.CommandParameters[c.CommandType]) {

                case SequenceCommandParameter.Bool:
                    return 1;

                case SequenceCommandParameter.None:
                    return 0;

                case SequenceCommandParameter.NoteParam:
                    return 3;

                case SequenceCommandParameter.OpenTrack:
                    return 2;

                case SequenceCommandParameter.Random:
                    return NumArguments((c.Parameter as RandomParameter).Command);

                case SequenceCommandParameter.S16:
                    return 1;

                case SequenceCommandParameter.Time:
                    return NumArguments((c.Parameter as TimeParameter).Command) + 1;

                case SequenceCommandParameter.TimeRandom:
                    return NumArguments((c.Parameter as RandomParameter).Command) + 1;

                case SequenceCommandParameter.TimeVariable:
                    return NumArguments((c.Parameter as VariableParameter).Command) + 1;

                case SequenceCommandParameter.U16:
                    return 1;

                case SequenceCommandParameter.U24:
                    return 1;

                case SequenceCommandParameter.U8:
                    return 1;

                case SequenceCommandParameter.S8:
                    return 1;

                case SequenceCommandParameter.U8S16:
                    return 2;

                case SequenceCommandParameter.Variable:
                    return NumArguments((c.Parameter as VariableParameter).Command);

                case SequenceCommandParameter.VariableLength:
                    return 1;

                case SequenceCommandParameter.If:
                    return NumArguments(c.Parameter as SequenceCommand);

            }

            //Default.
            return 0;

        }

        /// <summary>
        /// Execute the next command in a track.
        /// </summary>
        /// <param name="i">The track index.</param>
        private void ExecuteNext(int i) {
            ExecuteCommand(Events[_tracks[i].CurEvent], i);
        }

        /// <summary>
        /// Execute a command.
        /// </summary>
        /// <param name="c">The command.</param>
        /// <param name="trackIndex">The track index.</param>
        private void ExecuteCommand(SequenceCommand c, int trackIndex) {

            //Get the track.
            Track track = _tracks[trackIndex];

            //Flags.
            bool increment = true;

            //Fetch arguments.
            int numArgs = NumArguments(c);
            int[] args = new int[numArgs];
            for (int i = 0; i < numArgs; i++) {
                args[i] = GetCommandParameter(c, i, _rand, Events);
            }

            //If variable type, then the last argument needs to be converted from a variable number.
            if (c.CommandType == SequenceCommands.Variable || c.CommandType == SequenceCommands.TimeVariable) {
                args[args.Length - 1] = GetVar(args[args.Length - 1], trackIndex);
            }

            //Get true command type.
            SequenceCommands trueCommandType = GetTrueCommandType(c);

            //If command.
            if (c.CommandType != SequenceCommands.If || track.VariableFlag) {
                //Switch the current command.
                switch (trueCommandType) {

                    //Note.
                    case SequenceCommands.Note: {
                        int duration = args[2];

                        int k = (int)args[0] + track.Transpose;
                        if (k < 0) {
                            k = 0;
                        } else if (k > 0x7F) {
                            k = 0x7F;
                        }
                        byte key = (byte)k;
                        PlayNote(track, key, (byte)args[1], duration);
                        track.PortamentoKey = key;
                        if (track.Mono) {
                            track.Rest = duration;
                            if (duration == 0) {
                                track.WaitingForNoteToFinishBeforeContinuingXD = true;
                            }
                        }
                        break;
                    }

                    //Wait.
                    case SequenceCommands.Wait:
                        track.Rest = args[0];
                        break;

                    //Program change.
                    case SequenceCommands.ProgramChange:
                        track.Voice = args[0];
                        break;

                    //Open track.
                    case SequenceCommands.OpenTrack:
                        if (trackIndex == 0) {
                            Track newTrack = _tracks[args[0]];
                            if (newTrack.Allocated && !newTrack.Enabled) {
                                newTrack.Enabled = true;
                                newTrack.CurEvent = args[1];
                            }
                        }
                        break;

                    //Jump.
                    case SequenceCommands.Jump:
                        track.CurEvent = args[0];
                        increment = false;
                        break;

                    //Call.
                    case SequenceCommands.Call:
                        if (track.CallStackDepth < 3) {
                            track.CallStack[track.CallStackDepth] = track.CurEvent + 1;
                            track.CallStackDepth++;
                            track.CurEvent = args[0];
                            increment = false;
                        }
                        break;

                    //Random.
                    case SequenceCommands.Random:
                    case SequenceCommands.Variable:
                    case SequenceCommands.If:
                    case SequenceCommands.Time:
                    case SequenceCommands.TimeRandom:
                    case SequenceCommands.TimeVariable:
                        throw new Exception("Gota messed up."); //This should NOT happen with the true command type.

                    //Hold.
                    case SequenceCommands.EnvHold:
                        track.Hold = (byte)args[0];
                        break;

                    //Bank select.
                    case SequenceCommands.BankSelect:
                        track.BankNum = args[0];
                        break;

                    //Pan.
                    case SequenceCommands.Pan:
                        track.Panpot = (sbyte)(args[0] - 0x40);
                        break;

                    //Volume.
                    case SequenceCommands.Volume:
                        track.Volume = (byte)args[0];
                        break;

                    //Main volume.
                    case SequenceCommands.MainVolume:
                        Volume = (byte)args[0];
                        break;

                    //Transpose.
                    case SequenceCommands.Transpose:
                        track.Transpose = (sbyte)args[0];
                        break;

                    //Pitch bend.
                    case SequenceCommands.PitchBend:
                        track.PitchBend = (sbyte)args[0];
                        break;

                    //Pitch bend.
                    case SequenceCommands.BendRange:
                        track.PitchBendRange = (byte)args[0];
                        break;

                    //Priority.
                    case SequenceCommands.Prio:
                        track.Priority = (byte)args[0];
                        break;

                    //Note wait.
                    case SequenceCommands.NoteWait:
                        track.Mono = args[0] > 0;
                        break;

                    //Tie.
                    case SequenceCommands.Tie:
                        track.Tie = args[0] > 0;
                        track.StopAllChannels();
                        break;

                    //Porta.
                    case SequenceCommands.Porta: {
                        int k = args[0] + track.Transpose;
                        if (k < 0) {
                            k = 0;
                        } else if (k > 0x7F) {
                            k = 0x7F;
                        }
                        track.PortamentoKey = (byte)k;
                        track.Portamento = true;

                        break;
                    }

                    //Mod depth.
                    case SequenceCommands.ModDepth:
                        track.LFODepth = (byte)args[0];
                        break;

                    //Mod speed.
                    case SequenceCommands.ModSpeed:
                        track.LFOSpeed = (byte)args[0];
                        break;

                    //Mod type.
                    case SequenceCommands.ModType:
                        track.LFOType = (LFOType)args[0];
                        break;

                    //Mod range.
                    case SequenceCommands.ModRange:
                        track.LFORange = (byte)args[0];
                        break;

                    //Porta switch.
                    case SequenceCommands.PortaSw:
                        track.Portamento = args[0] > 0;
                        break;

                    //Porta time.
                    case SequenceCommands.PortaTime:
                        track.PortamentoTime = (byte)args[0];
                        break;

                    //Attack.
                    case SequenceCommands.Attack:
                        track.Attack = (byte)args[0];
                        break;

                    //Decay.
                    case SequenceCommands.Decay:
                        track.Decay = (byte)args[0];
                        break;

                    //Sustain.
                    case SequenceCommands.Sustain:
                        track.Sustain = (byte)args[0];
                        break;

                    //Release.
                    case SequenceCommands.Release:
                        track.Release = (byte)args[0];
                        break;

                    //Loop start.
                    case SequenceCommands.LoopStart:
                        if (track.CallStackDepth < 3) {
                            track.CallStack[track.CallStackDepth] = track.CurEvent;
                            track.CallStackLoops[track.CallStackDepth] = (byte)args[0];
                            track.CallStackDepth++;
                        }
                        break;

                    //Volume 2.
                    case SequenceCommands.Volume2:
                        track.Expression = (byte)args[0];
                        break;

                    //Print var.
                    case SequenceCommands.PrintVar:
                        Console.WriteLine("Variable " + args[0] + " = " + GetVar(args[0], trackIndex));
                        break;

                    //Mod delay.
                    case SequenceCommands.ModDelay:
                        track.LFODelay = (ushort)args[0];
                        break;

                    //Tempo.
                    case SequenceCommands.Tempo:
                        _tempo = (ushort)args[0];
                        break;

                    //Sweep pitch.
                    case SequenceCommands.SweepPitch:
                        track.SweepPitch = (short)args[0];
                        break;

                    //Loop end.
                    case SequenceCommands.LoopEnd:
                        if (track.CallStackDepth != 0) {
                            byte count = track.CallStackLoops[track.CallStackDepth - 1];
                            if (count != 0) {
                                count--;
                                if (count == 0) {
                                    track.CallStackDepth--;
                                    break;
                                }
                            }
                            track.CallStackLoops[track.CallStackDepth - 1] = count;
                            track.CurEvent = track.CallStack[track.CallStackDepth - 1];
                            increment = false;
                        }
                        break;

                    //Return.
                    case SequenceCommands.Return:
                        if (track.CallStackDepth != 0) {
                            track.CallStackDepth--;
                            track.CurEvent = track.CallStack[track.CallStackDepth];
                            increment = false;
                        }
                        break;

                    //Allocate tracks.
                    case SequenceCommands.AllocateTrack:
                        if (track.Index == 0) {
                            for (int i = 0; i < 0x10; i++) {
                                if ((args[0] & (1 << i)) != 0) {
                                    _tracks[i].Allocated = true;
                                }
                            }
                        }
                        break;

                    //Fin.
                    case SequenceCommands.Fin:
                        track.Stopped = true;
                        increment = false;
                        break;

                    //Set var.
                    case SequenceCommands.SetVar:
                        SetVar(args[0], trackIndex, (short)args[1]);
                        break;

                    //Add var.
                    case SequenceCommands.AddVar:
                        SetVar(args[0], trackIndex, (short)(GetVar(args[0], trackIndex) + args[1]));
                        break;

                    //Sub var.
                    case SequenceCommands.SubVar:
                        SetVar(args[0], trackIndex, (short)(GetVar(args[0], trackIndex) - args[1]));
                        break;

                    //Mul var.
                    case SequenceCommands.MulVar:
                        SetVar(args[0], trackIndex, (short)(GetVar(args[0], trackIndex) * args[1]));
                        break;

                    //Div var.
                    case SequenceCommands.DivVar:
                        SetVar(args[0], trackIndex, (short)(GetVar(args[0], trackIndex) / args[1]));
                        break;

                    //Shift var.
                    case SequenceCommands.ShiftVar:
                        SetVar(args[0], trackIndex, args[1] < 0 ? (short)(GetVar(args[0], trackIndex) >> -args[1]) : (short)(GetVar(args[0], trackIndex) << args[1]));
                        break;

                    //Rand var.
                    case SequenceCommands.RandVar: {
                        bool negate = false;
                        if (args[1] < 0) {
                            negate = true;
                            args[1] = (short)-args[1];
                        }
                        short val = (short)_rand.Next(args[1] + 1);
                        if (negate) {
                            val = (short)-val;
                        }
                        SetVar(args[0], trackIndex, val);

                        break;
                    }

                    //And var.
                    case SequenceCommands.AndVar:
                        SetVar(args[0], trackIndex, (short)(GetVar(args[0], trackIndex) & args[1]));
                        break;

                    //Or var.
                    case SequenceCommands.OrVar:
                        SetVar(args[0], trackIndex, (short)(GetVar(args[0], trackIndex) | (short)args[1]));
                        break;

                    //Xor var.
                    case SequenceCommands.XorVar:
                        SetVar(args[0], trackIndex, (short)(GetVar(args[0], trackIndex) ^ args[1]));
                        break;

                    //Not var.
                    case SequenceCommands.NotVar:
                        SetVar(args[0], trackIndex, (short)((~(GetVar(args[0], trackIndex) & args[1])) | (GetVar(args[0], trackIndex) & (~args[0]))));
                        break;

                    //Mod var.
                    case SequenceCommands.ModVar:
                        SetVar(args[0], trackIndex, (short)(GetVar(args[0], trackIndex) % args[1]));
                        break;

                    //Compare equal.
                    case SequenceCommands.CmpEq:
                        track.VariableFlag = GetVar(args[0], trackIndex) == args[1];
                        break;

                    //Compare greater than or equal.
                    case SequenceCommands.CmpGe:
                        track.VariableFlag = GetVar(args[0], trackIndex) >= args[1];
                        break;

                    //Compare greater than.
                    case SequenceCommands.CmpGt:
                        track.VariableFlag = GetVar(args[0], trackIndex) > args[1];
                        break;

                    //Compare less than or equal.
                    case SequenceCommands.CmpLe:
                        track.VariableFlag = GetVar(args[0], trackIndex) <= args[1];
                        break;

                    //Compare less than.
                    case SequenceCommands.CmpLt:
                        track.VariableFlag = GetVar(args[0], trackIndex) < args[1];
                        break;

                    //Compare not equal.
                    case SequenceCommands.CmpNe:
                        track.VariableFlag = GetVar(args[0], trackIndex) != args[1];
                        break;

                    //Usercall does nothing.
                    case SequenceCommands.UserCall:
                        break;

                    //Not implemented.
                    case SequenceCommands.Timebase:
                    case SequenceCommands.Monophonic:
                    case SequenceCommands.VelocityRange:
                    case SequenceCommands.BiquadType:
                    case SequenceCommands.BiquadValue:
                    case SequenceCommands.ModPhase:
                    case SequenceCommands.ModCurve:
                    case SequenceCommands.FrontBypass:
                    case SequenceCommands.SurroundPan:
                    case SequenceCommands.LpfCutoff:
                    case SequenceCommands.FxSendA:
                    case SequenceCommands.FxSendB:
                    case SequenceCommands.MainSend:
                    case SequenceCommands.InitPan:
                    case SequenceCommands.Mute:
                    case SequenceCommands.FxSendC:
                    case SequenceCommands.Damper:
                    case SequenceCommands.ModPeriod:
                    case SequenceCommands.EnvReset:
                    case SequenceCommands.Mod2Curve:
                    case SequenceCommands.Mod2Phase:
                    case SequenceCommands.Mod2Depth:
                    case SequenceCommands.Mod2Speed:
                    case SequenceCommands.Mod2Type:
                    case SequenceCommands.Mod2Range:
                    case SequenceCommands.Mod2Delay:
                    case SequenceCommands.Mod2Period:
                    case SequenceCommands.Mod3Curve:
                    case SequenceCommands.Mod3Phase:
                    case SequenceCommands.Mod3Depth:
                    case SequenceCommands.Mod3Speed:
                    case SequenceCommands.Mod3Type:
                    case SequenceCommands.Mod3Range:
                    case SequenceCommands.Mod3Delay:
                    case SequenceCommands.Mod3Period:
                    case SequenceCommands.Mod4Curve:
                    case SequenceCommands.Mod4Phase:
                    case SequenceCommands.Mod4Depth:
                    case SequenceCommands.Mod4Speed:
                    case SequenceCommands.Mod4Type:
                    case SequenceCommands.Mod4Range:
                    case SequenceCommands.Mod4Delay:
                    case SequenceCommands.Mod4Period:
                        Console.WriteLine("Command not implemented!");
                        break;

                }
            }

            //If the index should be incremented.
            if (increment) {
                track.CurEvent++;
            }

        }

        /// <summary>
        /// Play the song.
        /// </summary>
        public void Play() {
            if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped) {
                Stop();
                InitEmulation();
                _tracks[0].CurEvent = currEventOverride;
                State = PlayerState.Playing;
                CreateThread();
            }
        }

        /// <summary>
        /// Pause the playback.
        /// </summary>
        public void Pause() {
            if (State == PlayerState.Playing) {
                State = PlayerState.Paused;
                WaitThread();
            } else if (State == PlayerState.Paused || State == PlayerState.Stopped) {
                State = PlayerState.Playing;
                CreateThread();
            }
        }

        /// <summary>
        /// Stop the player.
        /// </summary>
        public void Stop() {
            if (State == PlayerState.Playing || State == PlayerState.Paused) {
                State = PlayerState.Stopped;
                WaitThread();
            }
        }

        /// <summary>
        /// Record to a file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        public void Record(string fileName) {
            _mixer.CreateWaveWriter(fileName);
            InitEmulation();
            _tracks[0].CurEvent = currEventOverride;
            State = PlayerState.Recording;
            CreateThread();
            WaitThread();
            _mixer.CloseWaveWriter();
        }

        /// <summary>
        /// Dispose of this.
        /// </summary>
        public void Dispose() {
            if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped) {
                State = PlayerState.ShutDown;
                WaitThread();
            }
        }

        /// <summary>
        /// Set ticks for things such as trackbar and fade out.
        /// </summary>
        void SetTicks() {

            //Count ticks.
            long[] totalTicks = new long[0x10];
            ReadTrackTicks(0, 0, currEventOverride, totalTicks);

            //Get maximum.
            MaxTicks = totalTicks.Max();
            _longestTrack = totalTicks.ToList().IndexOf(MaxTicks);

        }

        /// <summary>
        /// Read total ticks.
        /// </summary>
        /// <param name="trackNum">Track number.</param>
        /// <param name="baseTicks">Base ticks.</param>
        /// <param name="currEvent">Current event.</param>
        /// <param name="totalTicks">Total ticks.</param>
        void ReadTrackTicks(int trackNum, long baseTicks, int currEvent, long[] totalTicks) {

            //Track parameters.
            bool noteWait = true;
            int[] callStack = new int[3];
            int callStackDepth = 0;
            List<int> readCommands = new List<int>();

            //Read commands.
            while (currEvent < Events.Count) {

                //Get command.
                var c = Events[currEvent];

                //Set ticks.
                if (c.Ticks[trackNum] == 0) {
                    c.Ticks[trackNum] = baseTicks;
                }

                //Fetch arguments.
                int numArgs = NumArguments(c);
                int[] args = new int[numArgs];
                for (int i = 0; i < numArgs; i++) {
                    args[i] = GetCommandParameter(c, i, _rand, Events);
                }

                //If variable type, then the last argument needs to be converted from a variable number.
                if (c.CommandType == SequenceCommands.Variable || c.CommandType == SequenceCommands.TimeVariable) {
                    args[args.Length - 1] = GetVar(args[args.Length - 1], trackNum);
                }

                //Get true command type.
                SequenceCommands trueCommandType = GetTrueCommandType(c);

                //Switch type.
                switch (trueCommandType) {
                    case SequenceCommands.OpenTrack:
                        ReadTrackTicks(args[0], baseTicks, args[1], totalTicks);
                        break;
                    case SequenceCommands.NoteWait:
                        noteWait = args[0] > 0;
                        break;
                    case SequenceCommands.Note:
                        if (noteWait) {
                            baseTicks += args[2];
                        }
                        break;
                    case SequenceCommands.Wait:
                        baseTicks += args[0];
                        break;
                    case SequenceCommands.Call:
                        if (callStackDepth < 3) {
                            callStack[callStackDepth] = currEvent + 1;
                            callStackDepth++;
                            readCommands.Add(currEvent);
                            currEvent = args[0];
                            continue;
                        }
                        break;
                    case SequenceCommands.Jump:
                        if (!readCommands.Contains(args[0])) {
                            currEvent = args[0];
                            readCommands.Add(currEvent);
                            continue;
                        }
                        break;
                    case SequenceCommands.Return:
                        if (callStackDepth != 0) {
                            callStackDepth--;
                            readCommands.Add(currEvent);
                            currEvent = callStack[callStackDepth];
                            continue;
                        }
                        break;
                    case SequenceCommands.Fin:
                        totalTicks[trackNum] = baseTicks;
                        return;
                }

                //Increment event.
                readCommands.Add(currEvent);
                currEvent++;

            }

        }

        /// <summary>
        /// Get the true command type of a command.
        /// </summary>
        /// <param name="s">The command.</param>
        /// <returns>The true command type.</returns>
        public static SequenceCommands GetTrueCommandType(SequenceCommand s) {

            //Switch type.
            switch (s.CommandType) {
                case SequenceCommands.Random:
                case SequenceCommands.TimeRandom:
                    return GetTrueCommandType((s.Parameter as RandomParameter).Command);
                case SequenceCommands.Variable:
                case SequenceCommands.TimeVariable:
                    return GetTrueCommandType((s.Parameter as VariableParameter).Command);
                case SequenceCommands.If:
                    return GetTrueCommandType(s.Parameter as SequenceCommand);
                case SequenceCommands.Time:
                    return GetTrueCommandType((s.Parameter as TimeParameter).Command);
            }

            //Default.
            return s.CommandType;

        }

        /// <summary>
        /// Set current position.
        /// </summary>
        /// <param name="ticks">Ticks.</param>
        public void SetCurrentPosition(long ticks) {
            if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped) {
                if (State == PlayerState.Playing) {
                    Pause();
                }
                InitEmulation();
                while (ElapsedTicks == ticks) {
                    while (_tempoStack >= 240) {
                        _tempoStack -= 240;
                        for (int i = 0; i < 0x10; i++) {
                            Track track = _tracks[i];
                            if (track.Enabled && !track.Stopped) {
                                track.Tick();
                                while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped) {
                                    ExecuteNext(i);
                                }
                            }
                        }
                        ElapsedTicks++;
                        if (ElapsedTicks == ticks) {
                            break;
                        }
                    }
                    // Does it matter if this happens one last time before the loop ends?
                    _tempoStack += _tempo;
                    _mixer.ChannelTick();
                    _mixer.EmulateProcess();
                }
                for (int i = 0; i < 0x10; i++) {
                    _tracks[i].StopAllChannels();
                }
                Pause();
            }
        }

        /// <summary>
        /// Get current position.
        /// </summary>
        /// <returns>The current song position.</returns>
        public long GetCurrentPosition() => ElapsedTicks;

    }

    /// <summary>
    /// Player state.
    /// </summary>
    public enum PlayerState : byte {
        Stopped = 0,
        Playing,
        Paused,
        Recording,
        ShutDown
    }

    /// <summary>
    /// Song ended event.
    /// </summary>
    public delegate void SongEndedEvent();

}
