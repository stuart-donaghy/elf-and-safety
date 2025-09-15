using System;

namespace ElfAndSafety.Services;

public class DuplicateUserException : Exception
{
    public DuplicateUserException(string message) : base(message) { }
}
