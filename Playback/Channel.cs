using GotaSoundIO.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib.Playback {

    /// <summary>
    /// Channel.
    /// </summary>
    public class Channel {

        /// <summary>
        /// Channel index.
        /// </summary>
        public readonly byte Index;

        //Public members.
        public Track Owner;
        public InstrumentType Type;
        public EnvelopeState State;
        public bool AutoSweep;
        public byte BaseKey;
        public byte Key;
        public byte NoteVelocity;
        public sbyte StartingPan;
        public sbyte Pan;
        public int SweepCounter;
        public int SweepLength;
        public short SweepPitch;
        public int Velocity; // The SEQ Player treats 0 as the 100% amplitude value and -92544 (-723*128) as the 0% amplitude value. The starting ampltitude is 0% (-92544).
        public byte Volume; // From 0x00-0x7F (Calculated from Utils)
        public ushort BaseTimer;
        public ushort Timer;
        public int NoteDuration;

        //ADSR.
        private byte _attack;
        private int _sustain;
        private int _hold; //TODO: ACTUALLY IMPLEMENT!!!
        private ushort _decay;
        private ushort _release;

        //Position and previous samples.
        private int _pos;
        private short _prevLeft;
        private short _prevRight;

        //PCM data.
        private RiffWave _wave;
        private int _waveSample;

        //PSG data.
        private byte _psgDuty;
        private int _psgCounter;

        //Noise data.
        private ushort _noiseCounter;

        /// <summary>
        /// Create a new channel.
        /// </summary>
        /// <param name="i">The channel index.</param>
        public Channel(byte i) {
            Index = i;
        }

        /// <summary>
        /// Start PCM.
        /// </summary>
        /// <param name="wave">The wave.</param>
        /// <param name="noteDuration">Note duration.</param>
        /// <param name="clockSpeed">System clock speed.</param>
        public void StartPCM(RiffWave wave, int noteDuration, uint clockSpeed) {
            Type = InstrumentType.PCM;
            _waveSample = 0;
            _wave = wave;
            BaseTimer = (ushort)(clockSpeed / _wave.SampleRate);
            Start(noteDuration);
        }

        /// <summary>
        /// Start PSG.
        /// </summary>
        /// <param name="duty">Duty cycle.</param>
        /// <param name="noteDuration">Note duration.</param>
        public void StartPSG(byte duty, int noteDuration) {
            Type = InstrumentType.PSG;
            _psgCounter = 0;
            _psgDuty = duty;
            BaseTimer = 8006;
            Start(noteDuration);
        }

        /// <summary>
        /// Start noise.
        /// </summary>
        /// <param name="noteLength">Noise length.</param>
        public void StartNoise(int noteLength) {
            Type = InstrumentType.Noise;
            _noiseCounter = 0x7FFF;
            BaseTimer = 8006;
            Start(noteLength);
        }

        /// <summary>
        /// Start the channel.
        /// </summary>
        /// <param name="noteDuration">Note duration.</param>
        private void Start(int noteDuration) {
            State = EnvelopeState.Attack;
            Velocity = -92544;
            _pos = 0;
            _prevLeft = _prevRight = 0;
            NoteDuration = noteDuration;
        }

        /// <summary>
        /// Stop the channel by removing it.
        /// </summary>
        public void Stop() {
            if (Owner != null) {
                Owner.Channels.Remove(this);
            }
            Owner = null;
            Volume = 0;
        }

        /// <summary>
        /// Sweep main.
        /// </summary>
        /// <returns>Sweep.</returns>
        public int SweepMain() {
            if (SweepPitch != 0 && SweepCounter < SweepLength) {
                int sweep = (int)(Math.BigMul(SweepPitch, SweepLength - SweepCounter) / SweepLength);
                if (AutoSweep) {
                    SweepCounter++;
                }
                return sweep;
            } else {
                return 0;
            }
        }

        /// <summary>
        /// Set the attack.
        /// </summary>
        /// <param name="a">The attack,</param>
        public void SetAttack(int a) {
            _attack = Utils.AttackTable[a];
        }

        /// <summary>
        /// Set the decay.
        /// </summary>
        /// <param name="d">The decay.</param>
        public void SetDecay(int d) {
            _decay = Utils.DecayTable[d];
        }

        /// <summary>
        /// Set the sustain.
        /// </summary>
        /// <param name="s">The sustain.</param>
        public void SetSustain(byte s) {
            _sustain = Utils.SustainTable[s];
        }

        /// <summary>
        /// Set the hold.
        /// </summary>
        /// <param name="s">The hold.</param>
        public void SetHold(byte s) {
            _hold = Utils.SustainTable[s];
        }

        /// <summary>
        /// Set the release.
        /// </summary>
        /// <param name="r">The release.</param>
        public void SetRelease(int r) {
            _release = Utils.DecayTable[r];
        }

        /// <summary>
        /// Step the envelope.
        /// </summary>
        public void StepEnvelope() {
            switch (State) {
                case EnvelopeState.Attack: {
                    Velocity = _attack * Velocity / 0xFF;
                    if (Velocity == 0) {
                        State = EnvelopeState.Decay;
                    }
                    break;
                }
                case EnvelopeState.Decay: {
                    Velocity -= _decay;
                    if (Velocity <= _sustain) {
                        State = EnvelopeState.Sustain;
                        Velocity = _sustain;
                    }
                    break;
                }
                case EnvelopeState.Release: {
                    Velocity -= _release;
                    if (Velocity < -92544) {
                        Velocity = -92544;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Emulate a tick process rather than doing it.
        /// </summary>
        public void EmulateProcess() {
            if (Timer != 0) {
                int numSamples = (_pos + 0x100) / Timer;
                _pos = (_pos + 0x100) % Timer;
                for (int i = 0; i < numSamples; i++) {
                    if (Type == InstrumentType.PCM && !_wave.Loops) {
                        if (_waveSample >= _wave.Audio.NumSamples) {
                            Stop();
                        } else {
                            _waveSample++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process the samples.
        /// </summary>
        /// <param name="left">The left sample.</param>
        /// <param name="right">The right sample.</param>
        public void Process(out short left, out short right) {
            if (Timer != 0) {
                int numSamples = (_pos + 0x100) / Timer;
                _pos = (_pos + 0x100) % Timer;
                // prevLeft and prevRight are stored because numSamples can be 0.
                for (int i = 0; i < numSamples; i++) {
                    short samp = 0;
                    short lSample = 1;
                    short rSample = 1;
                    switch (Type) {
                        case InstrumentType.PCM: {
                            if (_wave != null) {
                                samp = 1;
                                if (_waveSample >= _wave.Audio.NumSamples) {
                                    if (_wave.Loops) {
                                        _waveSample = (int)_wave.LoopStart;
                                    } else {
                                        left = right = _prevLeft = _prevRight = 0;
                                        Stop();
                                        return;
                                    }
                                }
                                _wave.Audio.ChangeBlockSize(-1);
                                if (_wave.Audio.Channels[0][0] as PCM16 != null) {
                                    if (_wave.Audio.Channels.Count > 1) {
                                        lSample = ((short[])(_wave.Audio.Channels[0][0] as PCM16).RawData())[_waveSample];
                                        rSample = ((short[])(_wave.Audio.Channels[1][0] as PCM16).RawData())[_waveSample++];
                                    } else {
                                        samp = ((short[])(_wave.Audio.Channels[0][0] as PCM16).RawData())[_waveSample++];
                                    }
                                } else if (_wave.Audio.Channels[0][0] as PCM8 != null) {
                                    if (_wave.Audio.Channels.Count > 1) {
                                        lSample = (short)((((byte[])(_wave.Audio.Channels[0][0] as PCM8).RawData())[_waveSample] - 128) << 8);
                                        rSample = (short)((((byte[])(_wave.Audio.Channels[1][0] as PCM8).RawData())[_waveSample++] - 128) << 8);
                                    } else {
                                        samp = (short)((((byte[])(_wave.Audio.Channels[0][0] as PCM8).RawData())[_waveSample++] - 128) << 8);
                                    }
                                } else {
                                    samp = 0;
                                }
                            }
                            break;
                        }
                        case InstrumentType.PSG: {
                            samp = _psgCounter <= _psgDuty ? short.MinValue : short.MaxValue;
                            _psgCounter++;
                            if (_psgCounter >= 8) {
                                _psgCounter = 0;
                            }
                            break;
                        }
                        case InstrumentType.Noise: {
                            if ((_noiseCounter & 1) != 0) {
                                _noiseCounter = (ushort)((_noiseCounter >> 1) ^ 0x6000);
                                samp = -0x7FFF;
                            } else {
                                _noiseCounter = (ushort)(_noiseCounter >> 1);
                                samp = 0x7FFF;
                            }
                            break;
                        }
                        default: samp = 0; break;
                    }
                    lSample = (short)(samp * lSample);
                    rSample = (short)(samp * rSample);
                    lSample = (short)(lSample * Volume / 0x7F);
                    rSample = (short)(rSample * Volume / 0x7F);
                    _prevLeft = (short)(lSample * (-Pan + 0x40) / 0x80);
                    _prevRight = (short)(rSample * (Pan + 0x40) / 0x80);
                }
            }
            left = _prevLeft;
            right = _prevRight;
        }

    }

    /// <summary>
    /// Envelope state.
    /// </summary>
    public enum EnvelopeState : byte {
        Attack,
        Hold,
        Decay,
        Sustain,
        Release
    }

    /// <summary>
    /// Instrument type.
    /// </summary>
    public enum InstrumentType : byte {
        PCM, PSG, Noise
    }

}
