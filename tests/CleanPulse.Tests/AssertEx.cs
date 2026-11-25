namespace CleanPulse.Tests;

internal static class AssertEx
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed (True): {message}");
        }
    }

    public static void False(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException($"Assertion failed (False): {message}");
        }
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Assertion failed (Equal): {message}. Expected <{expected}>, Actual <{actual}>.");
        }
    }
}
