using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabNation.Interfaces
{
    public enum DecoderOutputColor { Green, Red, DarkRed, Orange, Yellow, Black, DarkPurple, Purple, Blue, DarkBlue}
    public enum DecoderIndex
    {
        DecoderOutputEvent = 0,
        DecoderOutputValue = 1
    }
    public enum DecoderTypeIndex
    {
        TypeByte = 0
    }

    public abstract class DecoderOutput
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string Text { get; private set; }
        public DecoderOutputColor Color { get; private set; }
        abstract public List<byte> Serialize();

        public DecoderOutput(int startIndex, int endIndex, DecoderOutputColor color, string text)
        {
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
            this.Text = text;
            this.Color = color;
        }

        protected List<byte> SerializeBase(int startIndex, int endIndex, string text)
        {
            List<byte> output = new List<byte>();

            //startIndex
            output.Add((byte)(startIndex >> 24));
            output.Add((byte)(startIndex >> 16));
            output.Add((byte)(startIndex >> 8));
            output.Add((byte)(startIndex));

            //endIndex
            output.Add((byte)(endIndex >> 24));
            output.Add((byte)(endIndex >> 16));
            output.Add((byte)(endIndex >> 8));
            output.Add((byte)(endIndex));

            //text
            int length = (int)Math.Min(255, text.Length);
            output.Add((byte)length);
            for (int i = 0; i < length; i++)
                output.Add((byte)(text.Substring(i, 1).ToCharArray()[0]));

            return output;
        }
    }

    public class DecoderOutputEvent : DecoderOutput
    {
        //this constructor for Event outputs
        public DecoderOutputEvent(int startLocation, int endLocation, DecoderOutputColor color, string eventName)
            : base(startLocation, endLocation, color, eventName) { }
        
        public override List<byte> Serialize()
        {
            List<byte> output = new List<byte>();
            
            //decoder index
            output.Add((byte)DecoderIndex.DecoderOutputEvent);            

            //base
            output.AddRange(SerializeBase(StartIndex, EndIndex, Text));

            return output;
        }
    }

    public class DecoderOutputValue<T> : DecoderOutput
    {
        public T Value { get; private set; }

        //these constructors for Value outputs        
        public DecoderOutputValue(int startLocation, int endLocation, DecoderOutputColor color, T value, string label)
            : base(startLocation, endLocation, color, label) 
        {
            this.Value = value;
        }

        public override List<byte> Serialize()
        {
            List<byte> output = new List<byte>();

            //decoder index
            output.Add((byte)DecoderIndex.DecoderOutputValue);

            //base
            output.AddRange(SerializeBase(StartIndex, EndIndex, Text));

            //valueType
            int byteSize = 0;
            if (typeof(T) == typeof(byte))
            {
                output.Add((byte)DecoderTypeIndex.TypeByte);
                byteSize = sizeof(byte);
            }

            //value
            T[] sourceArray = new T[1] { Value };
            byte[] destArray = new byte[byteSize];
            Buffer.BlockCopy(sourceArray, 0, destArray, 0, byteSize);

            return output;
        }
    }    
}
