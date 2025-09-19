using System;

namespace Domore.Conf; 
internal sealed class ConfPopulatedCallbackSignatureInvalidException : Exception {
    public sealed override string Message =>
        "The signature of the callback is invalid.";
}
