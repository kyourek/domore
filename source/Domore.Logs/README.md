*Domore.Logs* is a lightweight, simple, and very opinionated logging library.

`Log.Conf.Configure(path)` loads a logging configuration file and enables hot reload. A failed configuration throws
and leaves `Log.Conf.Configured` false. Calling `Logging.Complete()` stops hot reload and clears the active
configuration, after which `Log.Conf.Configure(path)` can be called again.

Log names and per-type settings use the full type name, falling back to the short type name when no full name is
available. The bounded background queue holds up to 1,024 pending items and drops the newest item when full; the
dropped count is reported to standard error when `Logging.Complete()` runs. Log service types named in configuration
are instantiated through reflection, so logging configuration files must be treated as trusted input.
