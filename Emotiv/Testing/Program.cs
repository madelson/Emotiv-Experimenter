using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Testing.Unit;

namespace MCAEmotiv.Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            ExtensionsTests.Run();
            ArrayTests.Run();
            ParameterTests.Run();
            ExampleTests.Run();
            ClassifierTests.Run();
            ChannelTests.Run();
            ControlTests.Run();
            SyncTests.Run();
            "Finished!".Print();

            while (true)
                System.Threading.Thread.Sleep(30000);
        }
    }
}
