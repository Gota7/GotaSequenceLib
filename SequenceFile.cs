using GotaSoundIO;
using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// Sequence file.
    /// </summary>
    public abstract class SequenceFile : IOFile {

        /// <summary>
        /// Commands.
        /// </summary>
        public List<SequenceCommand> Commands = new List<SequenceCommand>();

        /// <summary>
        /// Labels.
        /// </summary>
        public Dictionary<string, uint> Labels = new Dictionary<string, uint>();

        /// <summary>
        /// Song name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Raw data.
        /// </summary>
        public byte[] RawData = new byte[0];

        /// <summary>
        /// If success to writing commands.
        /// </summary>
        public bool WritingCommandSuccess { get; protected set; } = true;

        /// <summary>
        /// The sequence platform.
        /// </summary>
        /// <returns>The platform.</returns>
        public abstract SequencePlatform Platform();

        /// <summary>
        /// Public labels.
        /// </summary>
        public Dictionary<string, int> PublicLabels { get; protected set; } = new Dictionary<string, int>();

        /// <summary>
        /// Other labels.
        /// </summary>
        public List<int> OtherLabels { get; protected set; } = new List<int>();

        /// <summary>
        /// Blank constructor.
        /// </summary>
        public SequenceFile() {}

        /// <summary>
        /// Reads the sequence file from a file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public SequenceFile(string filePath) : base(filePath) {}

        /// <summary>
        /// Read command data into the commands list.
        /// </summary>
        /// <param name="globalMode">If to make private labels global.</param>
        public void ReadCommandData(bool globalMode = false) {

            //New reader.
            using (MemoryStream src = new MemoryStream(RawData)) {
                using (FileReader r = new FileReader(src)) {

                    //Command index.
                    int commandInd = 0;

                    //Platform.
                    var p = Platform();

                    //Offsets to command index.
                    Dictionary<uint, int> offsetMap = new Dictionary<uint, int>();

                    //Commands.
                    Commands = new List<SequenceCommand>();

                    //Labels.
                    PublicLabels = new Dictionary<string, int>();
                    OtherLabels = new List<int>();

                    //Read until end.
                    while (r.Position < RawData.Length) {

                        //Add command index.
                        offsetMap.Add((uint)r.Position, commandInd);

                        //MIDI2SSEQ fix because it doesn't understand that jumps use UInt24, not UInt32.
                        if (r.Position < RawData.Length - 1 && Commands.Count > 0 && Commands.Last().CommandType == SequenceCommands.Jump) {
                            long bak = r.Position;
                            if (r.ReadByte() == 0) {
                                continue;
                            } else {
                                r.Position = bak;
                            }
                        }

                        //Read the command.
                        SequenceCommand c = new SequenceCommand();
                        c.Read(r, p);
                        Commands.Add(c);
                        commandInd++;

                    }

                    //Public labels.
                    for (int i = 0; i < Labels.Count; i++) {
                        PublicLabels.Add(Labels.Keys.ElementAt(i), offsetMap[Labels.Values.ElementAt(i)]);
                    }

                    //Get labels.
                    for (int i = 0; i < Commands.Count; i++) {

                        //Command index.
                        int commandIndex = 0;

                        //Switch the type.
                        SequenceCommands trueType = Playback.Player.GetTrueCommandType(Commands[i]);
                        switch (trueType) {

                            //It has an offset.
                            case SequenceCommands.Call:
                            case SequenceCommands.Jump:
                            case SequenceCommands.OpenTrack:
                                commandIndex = SetOffsetIndex(Commands[i], offsetMap);
                                break;

                        }

                        //Label.
                        string label = "";

                        //Possible type.
                        if (trueType == SequenceCommands.Call || trueType == SequenceCommands.Jump || trueType == SequenceCommands.OpenTrack) {

                            //Get label.
                            uint offset = offsetMap.FirstOrDefault(x => x.Value == commandIndex).Key;
                            if (Labels.ContainsValue(offset)) {
                                label = Labels.FirstOrDefault(x => x.Value == offset).Key;
                            } else {
                                label = (globalMode ? "C" : "_c") + "ommand_" + commandIndex;
                                OtherLabels.Add(commandIndex);
                            }

                        }

                        //Set label.
                        switch (trueType) {

                            //Set the label.
                            case SequenceCommands.Call:
                            case SequenceCommands.Jump:
                            case SequenceCommands.OpenTrack:
                                SetCommandLabel(Commands[i], label);
                                break;

                        }

                    }

                    //Set reference commands.
                    for (int i = 0; i < Commands.Count; i++) {
                        SequenceCommands trueType = Playback.Player.GetTrueCommandType(Commands[i]);
                        switch (trueType) {
                            case SequenceCommands.Call:
                            case SequenceCommands.Jump:
                            case SequenceCommands.OpenTrack:
                                SetReferenceCommand(Commands[i]);
                                break;
                        }
                    }

                }

            }

        }

        /// <summary>
        /// Write command data.
        /// </summary>
        public void WriteCommandData() {

            //Output stream.
            using (MemoryStream o = new MemoryStream()) {
                using (FileWriter w = new FileWriter(o)) {

                    //Platform.
                    var p = Platform();

                    //Convert indices to offsets.
                    Dictionary<int, uint> indexMap = new Dictionary<int, uint>();
                    int commandInd = 0;
                    foreach (var c in Commands) {
                        indexMap.Add(commandInd, (uint)w.Position);
                        if (c.CommandType == SequenceCommands.Note || p.CommandMap().ContainsKey(c.CommandType) || p.ExtendedCommands().ContainsKey(c.CommandType)) {
                            c.Write(w, p);
                        }
                        commandInd++;
                    }

                    //Set position back.
                    w.Position = 0;

                    //Fix labels.
                    Labels = new Dictionary<string, uint>();
                    for (int i = 0; i < PublicLabels.Count; i++) {
                        Labels.Add(PublicLabels.Keys.ElementAt(i), indexMap[PublicLabels.Values.ElementAt(i)]);
                    }

                    //Fix commands.
                    for (int i = 0; i < Commands.Count; i++) {
                        SequenceCommands trueCommandType = Playback.Player.GetTrueCommandType(Commands[i]);
                        switch (trueCommandType) {
                            case SequenceCommands.Call:
                            case SequenceCommands.Jump:
                            case SequenceCommands.OpenTrack:
                                SetIndexOffset(Commands[i], indexMap);
                                break;
                        }
                    }

                    //Write every command for real now.
                    foreach (var c in Commands) {

                        //Only if supported command.
                        if (c.CommandType == SequenceCommands.Note || p.CommandMap().ContainsKey(c.CommandType) || p.ExtendedCommands().ContainsKey(c.CommandType)) {
                            c.Write(w, p);
                        }

                    }

                    //Set data.
                    RawData = o.ToArray();

                }

            }

        }

        /// <summary>
        /// Set the index for a command from an offset.
        /// </summary>
        /// <param name="c">The command.</param>
        /// <param name="offsetMap">The offset map.</param>
        public int SetOffsetIndex(SequenceCommand c, Dictionary<uint, int> offsetMap) {
            switch (c.CommandType) {
                case SequenceCommands.Random:
                case SequenceCommands.TimeRandom:
                    return SetOffsetIndex((c.Parameter as RandomParameter).Command, offsetMap);
                case SequenceCommands.If:
                    return SetOffsetIndex(c.Parameter as SequenceCommand, offsetMap);
                case SequenceCommands.Variable:
                case SequenceCommands.TimeVariable:
                    return SetOffsetIndex((c.Parameter as VariableParameter).Command, offsetMap);
                case SequenceCommands.Time:
                    return SetOffsetIndex((c.Parameter as TimeParameter).Command, offsetMap);
                case SequenceCommands.Jump:
                case SequenceCommands.Call:
                    (c.Parameter as UInt24Parameter).m_Index = offsetMap[(c.Parameter as UInt24Parameter).Offset];
                    return (c.Parameter as UInt24Parameter).m_Index;
                case SequenceCommands.OpenTrack:
                    (c.Parameter as OpenTrackParameter).m_Index = offsetMap[(c.Parameter as OpenTrackParameter).Offset];
                    return (c.Parameter as OpenTrackParameter).m_Index;
            }
            return -1;
        }

        /// <summary>
        /// Set the index for a command from an offset.
        /// </summary>
        /// <param name="c">The command.</param>
        /// <param name="label">The label.</param>
        public int SetCommandLabel(SequenceCommand c, string label) {
            switch (c.CommandType) {
                case SequenceCommands.Random:
                case SequenceCommands.TimeRandom:
                    SetCommandLabel((c.Parameter as RandomParameter).Command, label);
                    break;
                case SequenceCommands.If:
                    SetCommandLabel(c.Parameter as SequenceCommand, label);
                    break;
                case SequenceCommands.Variable:
                case SequenceCommands.TimeVariable:
                    SetCommandLabel((c.Parameter as VariableParameter).Command, label);
                    break;
                case SequenceCommands.Time:
                    SetCommandLabel((c.Parameter as TimeParameter).Command, label);
                    break;
                case SequenceCommands.Jump:
                case SequenceCommands.Call:
                    (c.Parameter as UInt24Parameter).Label = label;
                    break;
                case SequenceCommands.OpenTrack:
                    (c.Parameter as OpenTrackParameter).Label = label;
                    break;
            }
            return -1;
        }

        /// <summary>
        /// Set the offset for a command from an index.
        /// </summary>
        /// <param name="c">The command.</param>
        /// <param name="offsetMap">The offset map.</param>
        public uint SetIndexOffset(SequenceCommand c, Dictionary<int, uint> offsetMap) {
            switch (c.CommandType) {
                case SequenceCommands.Random:
                case SequenceCommands.TimeRandom:
                    return SetIndexOffset((c.Parameter as RandomParameter).Command, offsetMap);
                case SequenceCommands.If:
                    return SetIndexOffset(c.Parameter as SequenceCommand, offsetMap);
                case SequenceCommands.Variable:
                case SequenceCommands.TimeVariable:
                    return SetIndexOffset((c.Parameter as VariableParameter).Command, offsetMap);
                case SequenceCommands.Time:
                    return SetIndexOffset((c.Parameter as TimeParameter).Command, offsetMap);
                case SequenceCommands.Jump:
                case SequenceCommands.Call:
                    (c.Parameter as UInt24Parameter).Offset = offsetMap[(c.Parameter as UInt24Parameter).m_Index];
                    return (c.Parameter as UInt24Parameter).Offset;
                case SequenceCommands.OpenTrack:
                    (c.Parameter as OpenTrackParameter).Offset = offsetMap[(c.Parameter as OpenTrackParameter).m_Index];
                    return (c.Parameter as OpenTrackParameter).Offset;
            }
            return 0xFFFFFFFF;
        }

        /// <summary>
        /// Set the reference command.
        /// </summary>
        /// <param name="c">The command to have its reference set.</param>
        public void SetReferenceCommand(SequenceCommand c) {
            switch (c.CommandType) {
                case SequenceCommands.Random:
                case SequenceCommands.TimeRandom:
                    SetReferenceCommand((c.Parameter as RandomParameter).Command);
                    break;
                case SequenceCommands.If:
                    SetReferenceCommand(c.Parameter as SequenceCommand);
                    break;
                case SequenceCommands.Variable:
                case SequenceCommands.TimeVariable:
                    SetReferenceCommand((c.Parameter as VariableParameter).Command);
                    break;
                case SequenceCommands.Time:
                    SetReferenceCommand((c.Parameter as TimeParameter).Command);
                    break;
                case SequenceCommands.Jump:
                case SequenceCommands.Call:
                    (c.Parameter as UInt24Parameter).ReferenceCommand = Commands[(c.Parameter as UInt24Parameter).Index(Commands)];
                    break;
                case SequenceCommands.OpenTrack:
                    (c.Parameter as OpenTrackParameter).ReferenceCommand = Commands[(c.Parameter as OpenTrackParameter).Index(Commands)];
                    break;
            }
        }

        /// <summary>
        /// Convert the file to text.
        /// </summary>
        /// <returns>The file as text.</returns>
        public string[] ToText() {

            //Command list.
            ReadCommandData();
            List<string> l = new List<string>();

            //Add header.
            l.Add(";;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;");
            l.Add(";");
            l.Add("; " + Name);
            l.Add(";     Generated By Gota's Sound Tools");
            l.Add(";");
            l.Add(";;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;");
            l.Add("");

            //For each command. Last one isn't counted.
            for (int i = 0; i < Commands.Count; i++) {

                //Add labels.
                bool labelAdded = false;
                var labels = PublicLabels.Where(x => x.Value == i).Select(x => x.Key);
                foreach (var label in labels) {
                    if (i != 0 && !labelAdded && Commands[i - 1].CommandType == SequenceCommands.Fin) {
                        l.Add(" ");
                    }
                    l.Add(label + ":");
                    labelAdded = true;
                }
                if (OtherLabels.Contains(i)) {
                    if (i != 0 && !labelAdded && Commands[i - 1].CommandType == SequenceCommands.Fin) {
                        l.Add(" ");
                    }
                    l.Add("_command_" + i + ":");
                    labelAdded = true;
                }

                //Add command.
                if (i < Commands.Count - 1) {
                    l.Add("\t" + Commands[i].ToString());
                }

            }

            //Return the list.
            return l.ToArray();

        }

        /// <summary>
        /// Create a sequence from text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void FromText(List<string> text) {

            //Success by default.
            WritingCommandSuccess = true;

            //Reset labels.
            PublicLabels = new Dictionary<string, int>();
            OtherLabels = new List<int>();
            Dictionary<string, int> privateLabels = new Dictionary<string, int>();
            List<int> labelLines = new List<int>();

            //Format text.
            List<string> t = text.ToList();
            int comNum = 0;
            for (int i = t.Count - 1; i >= 0; i--) {
                t[i] = t[i].Replace("\t", " ").Replace("\r", "").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ");
                try { t[i] = t[i].Split(';')[0]; } catch { }
                if (t[i].Replace(" ", "").Length == 0) { t.RemoveAt(i); continue; }
                for (int j = 0; j < t[i].Length; j++) {
                    if (t[i][j].Equals(' ')) {
                        t[i] = t[i].Substring(j + 1);
                        j--;
                    } else {
                        break;
                    }
                }
            }

            //Fetch labels.
            for (int i = 0; i < t.Count; i++) {

                //If it's a label.
                if (t[i].EndsWith(":")) {
                    labelLines.Add(i);
                    if (t[i].StartsWith("_")) {
                        privateLabels.Add(t[i].Replace(":", ""), comNum);
                        OtherLabels.Add(comNum);
                    } else {
                        PublicLabels.Add(t[i].Replace(":", ""), comNum);
                    }
                } else {
                    comNum++;
                }

            }

            //Sort labels.
            PublicLabels = PublicLabels.OrderBy(obj => new NullTerminatedString(obj.Key)).ToDictionary(obj => obj.Key, obj => obj.Value);

            //Get commands.
            Commands = new List<SequenceCommand>();
            for (int i = 0; i < t.Count; i++) {
                if (labelLines.Contains(i)) {
                    continue;
                }
                SequenceCommand seq = new SequenceCommand();
                try { seq.FromString(t[i], PublicLabels, privateLabels); } catch (Exception e) { WritingCommandSuccess = false; throw new Exception("Command " + i + ": \"" + t[i] + "\" is invalid.", e); }
                Commands.Add(seq);
            }

            //Set reference commands.
            for (int i = 0; i < Commands.Count; i++) {
                SequenceCommands trueType = Playback.Player.GetTrueCommandType(Commands[i]);
                switch (trueType) {
                    case SequenceCommands.Call:
                    case SequenceCommands.Jump:
                    case SequenceCommands.OpenTrack:
                        SetReferenceCommand(Commands[i]);
                        break;
                }
            }

            //Fin.
            Commands.Add(new SequenceCommand() { CommandType = SequenceCommands.Fin });
            WriteCommandData();

        }

        /// <summary>
        /// Create a sequence from an MIDI.
        /// </summary>
        /// <param name="filePath">The MIDI path.</param>
        /// <param name="timeBase">Time base.</param>
        public void FromMIDI(string filePath, int timeBase = 48, bool privateLabelsForCalls = false) {
            Sanford.Multimedia.Midi.Sequence s = new Sanford.Multimedia.Midi.Sequence(filePath);
            Dictionary<string, int> pub;
            List<int> priv;
            Commands = SMF.ToSequenceCommands(s, out pub, out priv, Path.GetFileNameWithoutExtension(filePath), timeBase);
            PublicLabels = pub;
            OtherLabels = priv;
            WriteCommandData();
        }

        /// <summary>
        /// Convert the file to an MIDI.
        /// </summary>
        /// <param name="filePath">Path to save the MIDI.</param>
        public void SaveMIDI(string filePath) {
            ReadCommandData();
            Sanford.Multimedia.Midi.Sequence s = SMF.FromSequenceCommands(Commands, 0);
            s.Save(filePath);
        }

        /// <summary>
        /// Copy from another sequence.
        /// </summary>
        /// <param name="other">Other sequence.</param>
        public void CopyFromOther(SequenceFile other) {
            other.ReadCommandData();
            Commands = other.Commands;
            PublicLabels = other.PublicLabels;
            OtherLabels = other.OtherLabels;
        }

    }

}
