﻿using System;
using System.Collections.Generic;

namespace Domore.Logs {
    public interface ILogEntry {
        public Type LogType { get; }
        public string LogName { get; }
        public DateTime LogDate { get; }
        public LogSeverity LogSeverity { get; }
        public IEnumerable<string> LogList { get; }
    }
}
