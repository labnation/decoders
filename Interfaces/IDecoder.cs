using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabNation.Interfaces
{
    public class DecoderDescription
    {
        public string Name;
        public string ShortName;
        public string Author;
        public Dictionary<string, Type> InputWaveformTypes;
        public DecoderParameter[] Parameters;
        public int VersionMajor;
        public int VersionMinor;
        public string Description;
    }

    public interface IDecoder
    {
        DecoderDescription Description { get; }
        DecoderOutput[] Decode(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod);
    }
}
