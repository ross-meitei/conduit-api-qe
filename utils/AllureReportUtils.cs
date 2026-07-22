using Allure.Net.Commons;

namespace Integration.Tests.utils;

/// <summary>
/// Assertion helpers that record each check as an Allure step and defer failures,
/// so one test run reports every broken expectation instead of only the first.
/// </summary>
/// <remarks>
/// Two things this type gets deliberately right, both of which it previously got
/// wrong:
/// <list type="bullet">
/// <item>
/// The deferral is real. The earlier version recorded the exception and then
/// rethrew it, so the first failure still aborted the test and the recorded list
/// was never read for anything - the "report every broken expectation at once"
/// promise in the summary was not implemented. Worse, the rethrown failure and
/// the <see cref="ThrowRecordedExceptions"/> call in Dispose reported the same
/// problem twice, as two separate failures.
/// </item>
/// <item>
/// State is <see cref="AsyncLocal{T}"/>, not <c>[ThreadStatic]</c>. Every test
/// here is async, so continuations resume on whatever pool thread is free; with
/// thread-static state the list a test wrote to was not reliably the list its
/// Dispose read, and assertions could go missing or leak between tests.
/// </item>
/// </list>
/// A failing check still marks its Allure step failed at the moment it happens,
/// so the report shows exactly where each one broke.
/// </remarks>
public static class AllureReportUtils
{
    private static readonly AsyncLocal<List<Exception>?> Recorded = new();

    public static void ClearRecordedExceptions() => Recorded.Value = new List<Exception>();

    /// <summary>
    /// Run <paramref name="assertion"/> as a named Allure step. A failure is
    /// recorded and execution continues, so the remaining checks in the test
    /// still run; <see cref="ThrowRecordedExceptions"/> then fails the test with
    /// all of them.
    /// </summary>
    public static void LogAndAssert<T>(T actual, Action<T> assertion, string description)
    {
        try
        {
            AllureApi.Step(description, () => assertion(actual));
        }
        catch (Exception ex)
        {
            // The step is already marked failed by Allure. Hold the exception so
            // the test can finish its remaining assertions and report them all.
            (Recorded.Value ??= new List<Exception>()).Add(ex);
        }
    }

    /// <summary>
    /// Fail the test if any deferred assertion failed. Call from Dispose so it
    /// runs whatever path the test took.
    /// </summary>
    public static void ThrowRecordedExceptions()
    {
        var recorded = Recorded.Value;
        Recorded.Value = null;
        if (recorded is { Count: > 0 })
        {
            throw new AggregateException(
                $"{recorded.Count} assertion(s) failed in this test.", recorded);
        }
    }
}
