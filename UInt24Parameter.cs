using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// UInt24 parameter.
    /// </summary>
    public class UInt24Parameter {

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
            return ReferenceCommand == null ? m_Index : ReferenceCommand.Index(commands);
        }
        public int m_Index;

        /// <summary>
        /// Label.
        /// </summary>
        public string Label;

    }

}
