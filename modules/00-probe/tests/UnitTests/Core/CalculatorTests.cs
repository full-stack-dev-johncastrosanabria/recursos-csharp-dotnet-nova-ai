using Shouldly;
using Training.Probe.Core;

namespace Training.Probe.Tests.Core;

public sealed class CalculatorTests
{
    [Fact]
    public void Answer_returns_forty_two()
    {
        Calculator.Answer().ShouldBe(42);
    }
}
