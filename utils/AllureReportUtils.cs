using Allure.Net.Commons;

namespace Integration.Tests.utils;

/// <summary>
/// Assertion helpers that record each check as an Allure step and defer failures
/// so a single test reports every broken expectation at once. Mirrors the
/// Keystone suite's AllureReportUtils.
/// </summary>
public static class AllureReportUtils
{
    [ThreadStatic]
    private static List<Exception>? _recorded;

    public static void ClearRecordedExceptions() => _recorded = new List<Exception>();

    public static void LogAndAssert<T>(T actual, Action<T> assertion, string description)
    {
        AllureApi.Step(description, () =>
        {
            try
            {
                assertion(actual);
            }
            catch (Exception ex)
            {
                (_recorded ??= new List<Exception>()).Add(ex);
                throw;
            }
        });
    }

    public static void ThrowRecordedExceptions()
    {
        var recorded = _recorded;
        _recorded = null;
        if (recorded is { Count: > 0 })
        {
            throw new AggregateException("One or more recorded assertions failed.", recorded);
        }
    }
}
