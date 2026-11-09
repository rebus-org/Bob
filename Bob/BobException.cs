namespace Bob;

/// <summary>
/// Exception for "expected" errors, whose message is shown to the user as-is (i.e. without a stack trace)
/// </summary>
public class BobException(string message) : Exception(message);
