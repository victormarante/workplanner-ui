﻿namespace Shared;

public class PdfException : Exception
{
    public PdfException(string message)
        :base(message)
    {}
}