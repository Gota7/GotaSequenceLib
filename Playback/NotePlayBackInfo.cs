using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib.Playback {

    /// <summary>
    /// Note playback info.
    /// </summary>
    public class NotePlayBackInfo {

        /// <summary>
        /// Wave Id. Duty cycle if PSG.
        /// </summary>
        public int WaveId;

        /// <summary>
        /// Wave archive Id.
        /// </summary>
        public int WarId;

        /// <summary>
        /// Instrument type.
        /// </summary>
        public InstrumentType InstrumentType;

        /// <summary>
        /// Attack.
        /// </summary>
        public byte Attack = 127;

        /// <summary>
        /// Decay.
        /// </summary>
        public byte Decay = 127;

        /// <summary>
        /// Sustain.
        /// </summary>
        public byte Sustain = 127;

        /// <summary>
        /// Hold.
        /// </summary>
        public byte Hold = 127;

        /// <summary>
        /// Release.
        /// </summary>
        public byte Release = 127;

        /// <summary>
        /// Base key.
        /// </summary>
        public byte BaseKey = 60;

        /// <summary>
        /// Pan.
        /// </summary>
        public byte Pan = 64;

    }

}
