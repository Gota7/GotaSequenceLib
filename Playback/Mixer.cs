using GotaSoundIO.Sound.Playback;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib.Playback {

    /// <summary>
    /// Mixer.
    /// </summary>
    public class Mixer : IDisposable {

        //Private members.
        private readonly float _samplesReciprocal;
        private readonly int _samplesPerBuffer;
        private bool _isFading;
        private long _fadeMicroFramesLeft;
        private float _fadePos;
        private float _fadeStepPerMicroframe;    

        /// <summary>
        /// Channels.
        /// </summary>
        public Channel[] Channels;

        /// <summary>
        /// Mutes.
        /// </summary>
        public bool[] Mutes = new bool[16];

        /// <summary>
        /// Volume.
        /// </summary>
        public float Volume = 1f;

        /// <summary>
        /// Sound buffer.
        /// </summary>
        private readonly BufferedWaveProvider _buffer;

        /// <summary>
        /// Wave player.
        /// </summary>
        private IWavePlayer _out;

        /// <summary>
        /// Create a new mixer.
        /// </summary>
        public Mixer() {

            // The sampling frequency of the mixer is 1.04876 MHz with an amplitude resolution of 24 bits, but the sampling frequency after mixing with PWM modulation is 32.768 kHz with an amplitude resolution of 10 bits.
            // - gbatek
            // I'm not using either of those because the samples per buffer leads to an overflow eventually
            const int sampleRate = 65456;
            _samplesPerBuffer = 341; // TODO
            _samplesReciprocal = 1f / _samplesPerBuffer;

            Channels = new Channel[0x10];
            for (byte i = 0; i < 0x10; i++) {
                Channels[i] = new Channel(i);
            }

            _buffer = new BufferedWaveProvider(new WaveFormat(sampleRate, 16, 2)) {
                DiscardOnBufferOverflow = true,
                BufferLength = _samplesPerBuffer * 64
            };
            Init(_buffer);

        }

        /// <summary>
        /// Initialize the player.
        /// </summary>
        /// <param name="waveProvider"></param>
        protected void Init(IWaveProvider waveProvider) {
            try {
                _out = new WasapiOut();
            } catch {
                _out = new NullWavePlayer();
            }
            _out.Init(waveProvider);
            _out.Play();
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose() {
            _out.Stop();
            _out.Dispose();
            CloseWaveWriter();
        }

        /// <summary>
        /// Allocate a channel.
        /// </summary>
        /// <param name="type">Instrument type.</param>
        /// <param name="track">Track.</param>
        /// <returns>The allocated channel.</returns>
        public Channel AllocateChannel(InstrumentType type, Track track) {
            ushort allowedChannels;
            switch (type) {
                case InstrumentType.PCM: allowedChannels = 0b1111111111111111; break; // All channels (0-15)
                case InstrumentType.PSG: allowedChannels = 0b0011111100000000; break; // Only 8 9 10 11 12 13
                case InstrumentType.Noise: allowedChannels = 0b1100000000000000; break; // Only 14 15
                default: return null;
            }
            int GetScore(Channel c) {
                // Free channels should be used before releasing channels which should be used before track priority
                return c.Owner == null ? -2 : c.State == EnvelopeState.Release ? -1 : c.Owner.Priority;
            }
            Channel nChan = null;
            for (int i = 0; i < 0x10; i++) {
                if ((allowedChannels & (1 << i)) != 0) {
                    Channel c = Channels[i];
                    if (nChan != null) {
                        int nScore = GetScore(nChan);
                        int cScore = GetScore(c);
                        if (cScore <= nScore && (cScore < nScore || c.Volume <= nChan.Volume)) {
                            nChan = c;
                        }
                    } else {
                        nChan = c;
                    }
                }
            }
            return nChan != null && track.Priority >= GetScore(nChan) ? nChan : null;
        }

        /// <summary>
        /// Tick a channel.
        /// </summary>
        public void ChannelTick() {
            for (int i = 0; i < 0x10; i++) {
                Channel chan = Channels[i];
                if (chan.Owner != null) {
                    chan.StepEnvelope();
                    if (chan.NoteDuration == 0 && !chan.Owner.WaitingForNoteToFinishBeforeContinuingXD) {
                        chan.State = EnvelopeState.Release;
                    }
                    int vol = Utils.SustainTable[chan.NoteVelocity] + chan.Velocity + chan.Owner.GetVolume();
                    int pitch = ((chan.Key - chan.BaseKey) << 6) + chan.SweepMain() + chan.Owner.GetPitch(); // "<< 6" is "* 0x40"
                    if (chan.State == EnvelopeState.Release && vol <= -92544) {
                        chan.Stop();
                    } else {
                        chan.Volume = Utils.GetChannelVolume(vol);
                        chan.Timer = Utils.GetChannelTimer(chan.BaseTimer, pitch);
                        int p = chan.StartingPan + chan.Owner.GetPan();
                        if (p < -0x40) {
                            p = -0x40;
                        } else if (p > 0x3F) {
                            p = 0x3F;
                        }
                        chan.Pan = (sbyte)p;
                    }
                }
            }
        }

        /// <summary>
        /// Begin fade in.
        /// </summary>
        public void BeginFadeIn() {
            _fadePos = 0f;
            _fadeMicroFramesLeft = (long)(10000 / 1000.0 * 192);
            _fadeStepPerMicroframe = 1f / _fadeMicroFramesLeft;
            _isFading = true;
        }

        /// <summary>
        /// Begin fade out.
        /// </summary>
        public void BeginFadeOut() {
            _fadePos = 1f;
            _fadeMicroFramesLeft = (long)(10000 / 1000.0 * 192);
            _fadeStepPerMicroframe = -1f / _fadeMicroFramesLeft;
            _isFading = true;
        }

        /// <summary>
        /// If fading.
        /// </summary>
        /// <returns>If it is fading.</returns>
        public bool IsFading() {
            return _isFading;
        }

        /// <summary>
        /// If it is done fading.
        /// </summary>
        /// <returns>If it is done fading.</returns>
        public bool IsFadeDone() {
            return _isFading && _fadeMicroFramesLeft == 0;
        }

        /// <summary>
        /// Reset fading.
        /// </summary>
        public void ResetFade() {
            _isFading = false;
            _fadeMicroFramesLeft = 0;
        }

        /// <summary>
        /// Wave writer.
        /// </summary>
        private WaveFileWriter _waveWriter;

        /// <summary>
        /// Create a wave writer.
        /// </summary>
        /// <param name="fileName">The file path to export the wave to.</param>
        public void CreateWaveWriter(string fileName) {
            _waveWriter = new WaveFileWriter(fileName, _buffer.WaveFormat);
        }

        /// <summary>
        /// Close the wave writer.
        /// </summary>
        public void CloseWaveWriter() {
            _waveWriter?.Dispose();
        }

        /// <summary>
        /// Emulate a process instead of actually doing it.
        /// </summary>
        public void EmulateProcess() {
            for (int i = 0; i < _samplesPerBuffer; i++) {
                for (int j = 0; j < 0x10; j++) {
                    Channel chan = Channels[j];
                    if (chan.Owner != null) {
                        chan.EmulateProcess();
                    }
                }
            }
        }

        /// <summary>
        /// Process the audio.
        /// </summary>
        /// <param name="output">If the audio should be output.</param>
        /// <param name="recording">If the audio is being recorded.</param>
        public void Process(bool output, bool recording) {
            float masterStep;
            float masterLevel;
            if (_isFading && _fadeMicroFramesLeft == 0) {
                masterStep = 0;
                masterLevel = 0;
            } else {
                float fromMaster = Volume;
                float toMaster = Volume;
                if (_fadeMicroFramesLeft > 0) {
                    const float scale = 10f / 6f;
                    fromMaster *= (_fadePos < 0f) ? 0f : (float)Math.Pow(_fadePos, scale);
                    _fadePos += _fadeStepPerMicroframe;
                    toMaster *= (_fadePos < 0f) ? 0f : (float)Math.Pow(_fadePos, scale);
                    _fadeMicroFramesLeft--;
                }
                masterStep = (toMaster - fromMaster) * _samplesReciprocal;
                masterLevel = fromMaster;
            }
            byte[] b = new byte[4];
            for (int i = 0; i < _samplesPerBuffer; i++) {
                int left = 0,
                    right = 0;
                for (int j = 0; j < 0x10; j++) {
                    Channel chan = Channels[j];
                    if (chan.Owner != null) {
                        bool muted = Mutes[chan.Owner.Index]; // Get mute first because chan.Process() can call chan.Stop() which sets chan.Owner to null
                        chan.Process(out short channelLeft, out short channelRight);
                        if (!muted) {
                            left += channelLeft;
                            right += channelRight;
                        }
                    }
                }
                float f = left * masterLevel;
                if (f < short.MinValue) {
                    f = short.MinValue;
                } else if (f > short.MaxValue) {
                    f = short.MaxValue;
                }
                left = (int)f;
                b[0] = (byte)left;
                b[1] = (byte)(left >> 8);
                f = right * masterLevel;
                if (f < short.MinValue) {
                    f = short.MinValue;
                } else if (f > short.MaxValue) {
                    f = short.MaxValue;
                }
                right = (int)f;
                b[2] = (byte)right;
                b[3] = (byte)(right >> 8);
                masterLevel += masterStep;
                if (output) {
                    _buffer.AddSamples(b, 0, 4);
                }
                if (recording) {
                    _waveWriter.Write(b, 0, 4);
                }
            }
        }

    }

}
