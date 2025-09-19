namespace Domore.Conf.Cli; 
public sealed class CliConversionException : CliException {
    private static string GetMessage(ConfValueConverterException innerException) {
        return innerException?.Message ?? "Invalid value";
    }

    public CliConversionException(ConfValueConverterException innerException) : base(GetMessage(innerException), innerException) {
    }
}
