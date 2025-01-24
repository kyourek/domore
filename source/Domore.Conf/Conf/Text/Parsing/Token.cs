namespace Domore.Conf.Text.Parsing {
    internal abstract class Token : IConfToken {
        private string Content;

        protected abstract string Create();

        public override string ToString() {
            return Content ?? Create();
        }

        string IConfToken.Content {
            get {
                if (Content == null) {
                    Content = Create();
                }
                return Content;
            }
        }
    }
}
