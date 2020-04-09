using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// Sequence platform.
    /// </summary>
    public abstract class SequencePlatform {

        /// <summary>
        /// Command map.
        /// </summary>
        /// <returns>The command map.</returns>
        public abstract Dictionary<SequenceCommands, byte> CommandMap();

        /// <summary>
        /// Extended commands.
        /// </summary>
        /// <returns>The extended command map.</returns>
        public abstract Dictionary<SequenceCommands, byte> ExtendedCommands();

        /// <summary>
        /// Sequence data byte order.
        /// </summary>
        /// <returns>The byte order of sequence data.</returns>
        public abstract ByteOrder SequenceDataByteOrder();

    }

}
