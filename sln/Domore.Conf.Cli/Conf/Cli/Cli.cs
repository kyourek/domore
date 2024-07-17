using Domore.Conf.Extensions;
using System;

namespace Domore.Conf.Cli {
    public static class Cli {
        private static CliSetup SetupObject {
            get => _SetupObject ??= new();
            set => _SetupObject = value;
        }
        private static CliSetup _SetupObject;

        private static T Validate<T>(T target) {
            if (null == target) throw new ArgumentNullException(nameof(target));
            var description = TargetDescription.Describe(target.GetType());
            var validations = description.Validations;
            try {
                foreach (var validation in validations) {
                    validation.Run(target);
                }
            }
            catch (CliValidationException) {
                throw;
            }
            catch (Exception ex) {
                throw new CliValidationException(nameof(CliValidationException), ex);
            }
            return target;
        }

        private static T Conf<T>(T target, string line) {
            if (null == target) throw new ArgumentNullException(nameof(target));
            var description = TargetDescription.Describe(target.GetType());
            var confLines = description.Conf(line);
            var conf = string.Join(Environment.NewLine, confLines);
            try {
                target.ConfFrom(conf, key: "");
            }
            catch (ConfValueConverterException ex) {
                throw new CliConversionException(ex);
            }
            return target;
        }

        internal static CliSetup Setup() {
            return SetupObject;
        }

        public static T Configure<T>(T target, string line) {
            return Validate(Conf(target, line));
        }

        public static string Display<T>(T target, CliDisplayOptions options) {
            if (null == target) throw new ArgumentNullException(nameof(target));
            var description = TargetDescription.Describe(target.GetType());
            var display = description.Display(options);
            return display;
        }

        public static string Display<T>(T target) {
            return Display(target, CliDisplayOptions.None);
        }

        public static string Manual<T>(T target, CliDisplayOptions options) {
            if (null == target) throw new ArgumentNullException(nameof(target));
            var description = TargetDescription.Describe(target.GetType());
            var manual = description.Manual(options);
            return manual;
        }

        public static string Manual<T>(T target) {
            return Manual(target, CliDisplayOptions.None);
        }

        public static void Setup(Action<CliSetup> set) {
            if (set == null) {
                SetupObject = null;
            }
            else {
                set(SetupObject);
            }
            TargetDescription.Clear();
        }
    }
}
