using Shouldly;
using Training.Module10.Core;

namespace Training.Module10.Tests.Core;

public sealed class RetryableErrorsTests
{
    [Theory]
    [InlineData("40001")]  // serialization_failure
    [InlineData("40P01")]  // deadlock_detected
    [InlineData("40003")]  // statement_completion_unknown
    public void Class_40_is_the_database_asking_you_to_try_again(string sqlState)
    {
        RetryableErrors.Classify(sqlState).ShouldBe(FailureKind.Retryable);
        RetryableErrors.ShouldRetry(sqlState).ShouldBeTrue();
    }

    [Theory]
    [InlineData("23505")]  // unique_violation
    [InlineData("23502")]  // not_null_violation
    [InlineData("23503")]  // foreign_key_violation
    public void Class_23_is_a_real_conflict_with_real_data(string sqlState)
    {
        // Retrying one of these produces the identical error, slower.
        RetryableErrors.Classify(sqlState).ShouldBe(FailureKind.Conflict);
        RetryableErrors.ShouldRetry(sqlState).ShouldBeFalse();
    }

    [Theory]
    [InlineData("42P01")]  // undefined_table
    [InlineData("42601")]  // syntax_error
    [InlineData("53300")]  // too_many_connections
    public void Everything_else_is_fatal(string sqlState)
    {
        RetryableErrors.Classify(sqlState).ShouldBe(FailureKind.Fatal);
    }

    [Fact]
    public void An_unrecognised_code_is_fatal_rather_than_retryable()
    {
        // The safe default. An error you do not understand is not one to repeat.
        RetryableErrors.Classify("XX999").ShouldBe(FailureKind.Fatal);
        RetryableErrors.ShouldRetry("XX999").ShouldBeFalse();
    }

    [Fact]
    public void A_malformed_code_does_not_throw()
    {
        RetryableErrors.Classify("").ShouldBe(FailureKind.Fatal);
        RetryableErrors.Classify("4").ShouldBe(FailureKind.Fatal);
    }
}
