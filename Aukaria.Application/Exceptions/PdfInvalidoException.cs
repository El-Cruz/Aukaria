namespace Aukaria.Application.Exceptions;

public sealed class PdfInvalidoException : Exception
{
    public PdfInvalidoException(string message) : base(message)
    {
    }
}