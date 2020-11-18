using Xunit;
using Moq;
using FsCheck;
using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FsCheckDemo
{
    public class Input
    {
        public string input1 { get; set; }
        public int input2 { get; set; }
        public bool input3 { get; set; }
        public Guid input4 { get; set; }
        public decimal input5 { get; set; }
    };

    public interface ISomeDependency {
        string DependencyFunction(Input input);
    }

    public class SomeDependency : ISomeDependency {
        public string DependencyFunction(Input input) {
            return input.input1 + input.input2.ToString() + input.input3.ToString() + input.input4.ToString() + input.input5.ToString();
        }
    }

    public class SomeApp {
        public string SomeFunction(Input input, ISomeDependency someDependency) {
            return someDependency.DependencyFunction(input);
        }
    }

    public class InputGenerators
    {
        public static Arbitrary<Input> InputGenerator()
        {
            return (from x in Gen.Choose(0, 1024)
                    from y in Arb.Generate<string>()
                    select new Input
                    {
                        input1 = y,
                        input2 = x,
                        input3 = x % 2 == 0,
                        input4 = Guid.NewGuid(),
                        input5 = (decimal)(x / 2.0)
                    }).ToArbitrary();
        }
    }

    public class UnitTests
    {

        public UnitTests() {
            var configuration = Configuration.Default;
            configuration.MaxNbOfTest = 1000000;
            configuration.QuietOnSuccess = true;
            true.ToProperty().Check(configuration);

            Arb.Register<FsCheckDemo.InputGenerators>();
        }

        public static IEnumerable<object[]> TestData()
        {
            for (int i = 0; i < 1000; i++)
            {
                yield return  new object[]
                {
                    new Input
                    {
                        input1 = "", // Provide a random string
                        input2 = i, // Provide an integer
                        input3 = i % 2 == 0, // Provide a bool
                        input4 = Guid.Empty, // Provide a guid
                        input5 = (decimal)(i / 2.0) // Provide a decimal
                    }
                };
            }
        }

        [Property]
        public void PropertyTest(Input input)
        {
            // Run the test as a property based test
            Test(input);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void RangeTest(Input input)
        {
            // Run the test as a theory
            Test(input);
        }

        private void Test(Input input)
        {
            // Create the mock of FsCheck.ISomeDependency
            Mock<FsCheckDemo.ISomeDependency> dependencyMock = new Mock<FsCheckDemo.ISomeDependency>();
            
            // Default behaviour is that the mock works fine
            dependencyMock.Setup(
                x => x.DependencyFunction(It.IsAny<Input>())
                ).Returns("Working!");

            // If input 2 = 666 then throw an exception
            dependencyMock.Setup(
                x => x.DependencyFunction(It.Is<Input>(x => x.input2 == 666)
                )
            ).Throws(
                    new System.Exception("The power of Christ compeles thee!")
                    );

            // Create an instance of FsCheck.SomeApp and inject the mock
            FsCheckDemo.SomeApp someApp = new FsCheckDemo.SomeApp();
            Assert.True(someApp.SomeFunction(input, dependencyMock.Object) == "Working!");
        }
    }
}
