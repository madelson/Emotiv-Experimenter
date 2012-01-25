using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Controls;

namespace MCAEmotiv.Testing.Unit
{
    class ControlTests
    {
        [Description("some description")]
        private interface Interface
        {
            string C { get; set; }
        }

        [Description("A", DisplayName = "B")]
        private class Foo
        {
            [Parameter("C")]
            public string C { get; set; }

            [Parameter("D")]
            public bool D { get; set; }

            [Parameter("E")]
            public double E { get; set; }

            [Parameter("F")]
            public int F { get; set; }

            [Parameter("G")]
            public Bar G { get; set; }
        }

        [Description("H", DisplayName = "I")]
        private class Bar : Interface
        {
            [Parameter("C")]
            public string C { get; set; }
        }

        public static void Run()
        {
            // test config panel
            var bar = new Bar() { C = "sub" };
            var foo = new Foo() { C = "c", D = true, E = 2.0, F = 2, G = bar };
            var config = new ConfigurationPanel(foo);
            var copy = config.GetConfiguredObject() as Foo;
            if (!bar.HasEqualParameters(copy.G))
                throw new Exception("Sub-object comparison failed!");
            foo.G = copy.G = null;
            if (!foo.HasEqualParameters(copy))
                throw new Exception("Main object comparison failed!");
            var fprop = typeof(Foo).GetProperty("F");
            config.SetterFor(fprop)(200);
            if ((int)config.GetterFor(fprop)() != 200)
                throw new Exception("Getter or setter failed!");
            config.PropertyChanged += (args) =>
            {
                if (args.Property != fprop)
                    return;

                if ((int)args.Getter() > 150)
                    args.Setter((int)args.Getter() - 2);
            };
            config.SetterFor(fprop)(190);
            if ((int)config.GetterFor(fprop)() != 150)
                throw new Exception("PropertyChanged failed!");

            // test derived type panel
            var derivedConfig = new DerivedTypeConfigurationPanel(typeof(Interface), bar);
            if (!new Bar() { C = "sub" }.HasEqualParameters((Bar)derivedConfig.GetConfiguredObject()))
                throw new Exception("DerivedTypeConfigurationPanel failed!");
            var diffBar = new Bar() { C = "different" };
            derivedConfig.SetConfiguredObject(diffBar);
            if (!diffBar.HasEqualParameters((Bar)derivedConfig.GetConfiguredObject()))
                throw new Exception("SetConfiguredObject failed!");
        }
    }
}
