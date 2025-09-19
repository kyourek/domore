using CONF = Domore.Conf.Conf;

namespace Domore.Conf.Logs; 
public sealed class LogConf {
    private LogConf() {
    }

    public static void ConfigureLogging(object source) {
        CONF.Contain(source).ConfigureLogging();
    }
}
