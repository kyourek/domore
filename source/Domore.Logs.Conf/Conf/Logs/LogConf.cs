using CONF = Domore.Conf.Conf;

namespace Domore.Conf.Logs; 
public static class LogConf {
    public static void ConfigureLogging(object source) {
        CONF.Contain(source).ConfigureLogging();
    }
}
