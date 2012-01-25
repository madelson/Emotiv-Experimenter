using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv;
using MCAEmotiv.Classification;

namespace MCAEmotiv.Testing.Unit
{
    static class ParameterTests
    {
        [Description("D", DisplayName = "?")]
        class Foo
        {
            [Parameter("S", DefaultValue = "Hello, World!")]
            public string S { get; set; }

            [Parameter("I", DefaultValue = 100, MinValue = 50, MaxValue = 200)]
            public int I { get; set; }

            public int NotParam { get; set; }
        }

        enum X
        {
            [Description("a")]
            A,
            [Description("b")]
            B,
            [Description("c")]
            C
        }

        public static void Run()
        {
            var foo = new Foo();
            var parameters = foo.GetParameters().ToIArray();

            // check count
            if (parameters.Count != 2)
                throw new Exception("Bad parameter count: " + parameters.Count);

            // check default values
            foo.SetParametersToDefaultValues();
            foreach (var param in parameters)
                if (param.HasDefaultValue() && !foo.GetProperty(param.Property).Equals(param.DefaultValue))
                    throw new Exception("Parameter " + param.Property.Name + " has value " + foo.GetProperty(param.Property) + " instead of default value " + param.DefaultValue);

            // check min/max
            foo.I = (int)parameters.Where(p => p.Description == "I").First().MinValue - 1;
            string errorMessage;
            if (foo.AreParameterValuesValid(out errorMessage))
                throw new Exception("MinValue check failed!");
            foo.I = (int)parameters.Where(p => p.Description == "I").First().MaxValue + 1;
            if (foo.AreParameterValuesValid(out errorMessage))
                throw new Exception("MaxValue check failed!");

            // check transfer
            foo.SetParametersToDefaultValues();
            var foo2 = new Foo();
            foo.TransferParameterValuesTo(foo2);
            foreach (var param in parameters)
                if (!foo.GetProperty(param.Property).Equals(foo2.GetProperty(param.Property)))
                    throw new Exception("Property transfer failed on " + param.Property.Name);

            // check factory
            foo.S += "suffix";
            var factory = foo.GetFactory();
            for (int i = 0; i < 3; i++)
            {
                foo2 = (Foo)factory();
                if (foo2.I != foo.I || foo2.S != foo.S)
                    throw new Exception("Factory failed!");
            }

            // check parameter equals
            foo.SetParametersToDefaultValues();
            foo2.SetParametersToDefaultValues();
            if (!foo.HasEqualParameters(foo2))
                throw new Exception("Equals check failed on equal objects");
            foo.S = "different";
            if (foo.HasEqualParameters(foo2))
                throw new Exception("Equals check failed on unequal objects");

            // check description
            if (typeof(Foo).DisplayName() != "?")
                throw new Exception("DisplayName failed!");
            if (typeof(Foo).Description() != "D")
                throw new Exception("Description failed!");

            // check enum
            foreach (Enum value in Enum.GetValues(typeof(X)))
                if (!value.GetDescriptionForEnum().Description.Equals(value.ToString().ToLower()))
                    throw new Exception("GetDescription for enum failed!");
        }
    }
}
