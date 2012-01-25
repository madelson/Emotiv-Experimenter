using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Interop;

namespace MCAEmotiv.Testing.Unit
{
    class ChannelTests
    {
        public static void Run()
        {
            if (Channels.Values.Count != 14
                || Channels.Values[0] != Channel.AF3
                || Channels.Values.LastItem() != Channel.AF4)
                throw new Exception("Values failed");

            for (int i = 0; i < Channels.Values.Count; i++)
                if (Channels.Values[i].ToIndex() != i)
                    throw new Exception("ToIndex failed");

            foreach (var ch in Channels.Values)
                if (ch.ToString() != ch.ToEdkChannel().ToString())
                    throw new Exception(ch.ToString() + " != " + ch.ToEdkChannel().ToString());
            if (Channels.Values.Select(ch => ch.ToEdkChannel()).Distinct().Count() != Channels.Values.Count)
                throw new Exception("Bad mapping to edk channels!");

            var mirrorSet = new HashSet<Channel>(Channels.Values.Select(c => c.Mirror()));
            if (!Channels.Values.Where(c => !mirrorSet.Contains(c)).IsEmpty())
                throw new Exception("A channel isn't mirrored");
            foreach (var channel in Channels.Values)
                if (channel.Mirror().Mirror() != channel)
                    throw new Exception("The mirror of a channel's mirror should be the channel itself");
        }
    }
}
