using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib.Playback {

    /// <summary>
    /// A playable bank.
    /// </summary>
    public interface PlayableBank {

        /// <summary>
        /// Get the note playback info for a certain note with a certain velocity.
        /// </summary>
        /// <param name="program">Program number.</param>
        /// <param name="note">Note to play.</param>
        /// <param name="velocity">Velocity of the note to play.</param>
        /// <returns>The note playback info.</returns>
        NotePlayBackInfo GetNotePlayBackInfo(int program, Notes note, byte velocity);

    }

}
