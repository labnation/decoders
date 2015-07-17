# Getting started

## Getting the files

Clone the repository

Generate the Visual Studio project files by running ```protobuild.exe```

## Writing a decoder

Open the solution and add your own **Class library** project, i.e. *MyDecoders*.

Add references to

1. LabNationInterfaces (Solution > Projects)
2. System.ComponentModel.Composition (Assemblies)

Write your decoder

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using LabNation.Interfaces;

namespace LabNation.Decoders
{
    [Export(typeof(IDecoder))]
    public class DecoderI2C : IDecoder
    {
        public DecoderDescription Description
        {
            get
            {
                return new DecoderDescription()
                {
                    Name = "Joy decoder",
                    ShortName = "Joy",
                    Author = "J. Lajoie",
                    VersionMajor = 0,
                    VersionMinor = 1,
                    Description = "A simple decoder to decode the joy in bits",
                    InputWaveformTypes = new Dictionary<string, Type>() 
                    {
                        { "Bit 0", typeof(bool)},
                        { "Bit 1", typeof(bool)},
                    },
                    Parameters = null
                };
            }
        }

        public DecoderOutput[] Decode(Dictionary<string, Array> inputWaveforms, Dictionary<string, object> parameters, double samplePeriod)
        {
            bool[] B0 = (bool[])inputWaveforms["Bit 0"];
            bool[] B1 = (bool[])inputWaveforms["Bit 1"];

            List<DecoderOutput> decoderOutputList = new List<DecoderOutput>();

            int lastUsedIndex = 0;
            for(int i = 1; i < B0.Length; i++)
            {
                DecoderOutput d = null;
                if(B0[i] != B0[lastUsedIndex] && B1[i] != B1[lastUsedIndex]) 
                    d = new DecoderOutputEvent(lastUsedIndex, i, DecoderOutputColor.Blue, "Both changed!");
                else if (B0[i] != B0[lastUsedIndex] && B1[i] == B1[lastUsedIndex])
                    d = new DecoderOutputEvent(lastUsedIndex, i, DecoderOutputColor.Yellow, "B0 changed!");
                else if (B0[i] == B0[lastUsedIndex] && B1[i] != B1[lastUsedIndex])
                    d = new DecoderOutputEvent(lastUsedIndex, i, DecoderOutputColor.Red, "B1 changed!");
                if (d != null)
                {
                    decoderOutputList.Add(d);
                    lastUsedIndex = d.EndIndex;
                }
            }

            return decoderOutputList.ToArray();
        }
    }
}
```

## Build and use

Build the project

Copy the output DLL from the build directory (i.e. ```./MyDecoders/bin/Debug/MyDecoders.dll```) to ```<My Documents>/LabNation/Plugins```

Restart the SmartScope app and enjoy your decoder

## DLL location
Our approach allows a DLL file compiled on any platform, to be used on any other platform. So a DLL file compiled on Windows can be used on iOS. Just make sure the DLL is placed in the correct folder, listed below:

| Platform        | Path          | 
| ------------- | ------------- |
| Mac           | ```/Users/<username>/LabNation/Plugins``` |
| Linux         | ```~/LabNation/Plugins```                 |
| Windows       | ```<My Documents>/LabNation/Plugins```    |
| Android       | ```<sd-card>/LabNation/Plugins```         |
| iOS           | See section below regarding DropBox       |

## Fetch DLL over DropBox
For all platforms, you can access a DLL file over DropBox. To do so, in the app go to Menu -> Add decoder -> Fetch from dropbox. If never done before, this will authenticate to DropBox and create all folders required. Next, on your PC you can save the DLL file to \Dropbox\Apps\LabNation SmartScope\Plugins. All DLL files you place here can now be accessed from all your devices over dropbox!

