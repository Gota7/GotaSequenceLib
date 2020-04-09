using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// Sequence command type.
    /// </summary>
    public enum SequenceCommandParameter {
        NoteParam,
        OpenTrack,
        VariableLength,
        U24,
        Random,
        Variable,
        If,
        Time,
        TimeRandom,
        TimeVariable,
        U8,
        S8,
        Bool,
        U16,
        S16,
        Extended,
        U8S16,
        None
    }

}
