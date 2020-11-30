using Xunit;
using Moq;
using FsCheck;
using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Xunit.Abstractions;
using NLog;

namespace FsCheckDemo
{
    public class SomeInput
    {
        public string Input1 { get; set; }
        public int Input2 { get; set; }
        public bool Input3 { get; set; }
        public Guid Input4 { get; set; }
        public decimal Input5 { get; set; }
    };

    public interface ISomeDependency {
        string DependencyFunction(SomeInput input);
    }

    public class SomeDependency : ISomeDependency {
        public string DependencyFunction(SomeInput input) {
            // Throw an exception if null
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return input.Input1 + 
                input.Input2.ToString(CultureInfo.CurrentCulture) + 
                input.Input3.ToString() + 
                input.Input4.ToString() + 
                input.Input5.ToString(CultureInfo.CurrentCulture);
        }
    }

    public static class SomeApp {
        public static string SomeFunction(SomeInput input, ISomeDependency someDependency) {
            // Throw an exception if null
            if (someDependency == null)
                throw new ArgumentNullException(nameof(someDependency));

            return someDependency.DependencyFunction(input);
        }
    }

    public class InputGenerators
    {
        public static Arbitrary<SomeInput> InputGenerator()
        {
            return (from x in Gen.Choose(0, 1024)
                    from y in Arb.Generate<string>()
                    select new SomeInput
                    {
                        Input1 = y,
                        Input2 = x,
                        Input3 = x % 2 == 0,
                        Input4 = Guid.NewGuid(),
                        Input5 = (decimal)(x / 2.0)
                    }).ToArbitrary();
        }
    }

    public class UnitTests : IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public UnitTests(ITestOutputHelper output)
        {

            var configuration = Configuration.Default;
            true.ToProperty().Check(configuration);

            Arb.Register<InputGenerators>();
        }

        public static IEnumerable<object[]> TestData()
        {
            for (int i = 0; i < 1000; i++)
            {
                yield return  new object[]
                {
                    new SomeInput
                    {
                        Input1 = "", // Provide a random string
                        Input2 = i, // Provide an integer
                        Input3 = i % 2 == 0, // Provide a bool
                        Input4 = Guid.Empty, // Provide a guid
                        Input5 = (decimal)(i / 2.0) // Provide a decimal
                    }
                };
            }
        }

        [Property(MaxTest = 100000)]
        public void PropertyTest(SomeInput input)
        {
            // Throw an exception if null
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            _logger.Info(input.Input2.ToString(CultureInfo.CurrentCulture));

            // Run the test as a property based test
            Test(input);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void RangeTest(SomeInput input)
        {
            // Throw an exception if null
            if (input == null) 
                throw new ArgumentNullException(nameof(input));

            _logger.Info(input.Input2.ToString(CultureInfo.CurrentCulture));

            // Run the test as a theory
            var ex = Record.Exception(() => Test(input));
            Assert.Null(ex);
        }

        private static void Test(SomeInput input)
        {
            // Create the mock of FsCheck.ISomeDependency
            Mock<ISomeDependency> dependencyMock = new Mock<ISomeDependency>();
            
            // Default behaviour is that the mock works fine
            dependencyMock.Setup(
                x => x.DependencyFunction(It.IsAny<SomeInput>())
                ).Returns("Working!");

            /*
            // If input 2 = 666 then throw an exception
            dependencyMock.Setup(
                x => x.DependencyFunction(It.Is<SomeInput>(x => x.Input2 == 666)
                )
            ).Throws(
                    new System.Exception("The power of Christ compeles thee!")
                    );
            */

            // Create an instance of FsCheck.SomeApp and inject the mock
            Assert.True(SomeApp.SomeFunction(input, dependencyMock.Object) == "Working!");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Cleanup
            LogManager.Shutdown();
        }
    }
}
