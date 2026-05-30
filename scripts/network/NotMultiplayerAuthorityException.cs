using System;

public class NotMultiplayerAuthorityException : Exception
{
    public NotMultiplayerAuthorityException() : base("Method ran on client!") { }
    public NotMultiplayerAuthorityException(string message) : base(message) { }
}