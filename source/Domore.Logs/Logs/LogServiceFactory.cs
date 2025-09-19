using System;

namespace Domore.Logs; 
internal sealed class LogServiceFactory {
    public ILogService Create(string typeName) {
        var type = Type.GetType(typeName, ignoreCase: true, throwOnError: false);
        if (type == null) {
            var internalTypeName = $"{typeof(LogServiceFactory).Namespace}.Service.{typeName}Log";
            var internalType = Type.GetType(internalTypeName, ignoreCase: true, throwOnError: false);
            if (internalType == null) {
                Logging.Notify($"Type not found [{typeName}]");
                return null;
            }
            type = internalType;
        }
        var obj = default(object);
        try {
            obj = Activator.CreateInstance(type);
        }
        catch {
            Logging.Notify($"Cannot create instance [{type}]");
            return null;
        }
        var inst = default(ILogService);
        try {
            inst = (ILogService)obj;
        }
        catch {
            Logging.Notify($"Cannot cast object to instance of {nameof(ILogService)} [{obj}]");
            return null;
        }
        return inst;
    }
}
