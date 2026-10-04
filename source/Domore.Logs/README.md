*Domore.Logs* is a lightweight, simple, and very opinionated logging library.

`Log.Conf.Configure(path)` loads a logging configuration file and enables hot reload. A failed configuration throws
and leaves `Log.Conf.Configured` false. Calling `Logging.Complete()` stops hot reload and clears the active
configuration, after which `Log.Conf.Configure(path)` can be called again.