using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;

namespace Domore.Conf {
    using Text;

    public class AppSettingsProvider : IConfContentProvider {
        private static TextContentProvider Text =>
            _Text ?? (
            _Text = new TextContentProvider());
        private static TextContentProvider _Text;

        private static IEnumerable<KeyValuePair<string, string>> EmptySettings {
            get { yield break; }
        }

        private static IEnumerable<KeyValuePair<string, string>> GetSettings(NameValueCollection collection) {
            if (collection == null) return EmptySettings;
            if (collection.HasKeys() == false) return EmptySettings;
            return collection.AllKeys.Select(key => new KeyValuePair<string, string>(key, collection[key]));
        }

        private static IEnumerable<KeyValuePair<string, string>> GetSettings(KeyValueConfigurationCollection collection) {
            if (collection == null) return EmptySettings;
            return collection.AllKeys.Select(key => new KeyValuePair<string, string>(key, collection[key].Value));
        }

        private static IEnumerable<KeyValuePair<string, string>> GetSettings(AppSettingsSection section) {
            if (section == null) return EmptySettings;
            return GetSettings(section.Settings);
        }

        private static IEnumerable<KeyValuePair<string, string>> GetSettings(string exePath) {
            return string.IsNullOrWhiteSpace(exePath)
                ? GetSettings(ConfigurationManager.AppSettings)
                : GetSettings(ConfigurationManager.OpenExeConfiguration(exePath)?.AppSettings);
        }

        private IEnumerable<KeyValuePair<string, string>> GetConfContents(object content) {
            return GetSettings(content?.ToString());
        }

        public ConfContent GetConfContent(object source) {
            var exePath = $"{source}";
            var settings = GetSettings(exePath);
            var text = string.Join(Environment.NewLine, settings.Select(set => string.Join(" = ", set.Key, set.Value)));
            var conf = Text.GetConfContent(text, new object[] {
                string.IsNullOrWhiteSpace(exePath)
                    ? $"{nameof(ConfigurationManager)}.{nameof(ConfigurationManager.AppSettings)}"
                    : exePath
            });
            return conf;
        }
    }
}
