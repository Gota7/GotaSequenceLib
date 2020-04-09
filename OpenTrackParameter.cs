using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// Open track parameter.
    /// </summary>
    public class OpenTrackParameter {

        /// <summary>
        /// Track number.
        /// </summary>
        public byte TrackNumber;

        /// <summary>
        /// Offset.
        /// </summary>
        public UInt24 Offset = 0;

        /// <summary>
        /// Reference command.
        /// </summary>
        public SequenceCommand ReferenceCommand;

        /// <summary>
        /// Command index used when reading and writing.
        /// </summary>
        /// <param name="commands">The commands.</param>
        public int Index(List<SequenceCommand> commands) {
            int ind = m_Index;
            if (ReferenceCommand != null) {
                if (ReferenceCommand.Index(commands) != -1) {
                    ind = ReferenceCommand.Index(commands);
                }
            }
            return ind;
        }
        public int m_Index;

        /// <summary>
        /// Label text.
        /// </summary>
        public string Label;

    }

}
