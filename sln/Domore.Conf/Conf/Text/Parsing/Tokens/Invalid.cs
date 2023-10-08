using System;

namespace Domore.Conf.Text.Parsing.Tokens {
    internal sealed class Invalid : Token {
        protected sealed override string Create() {
            throw new NotSupportedException();
        }
    }
}
