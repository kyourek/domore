using Domore.Conf.Extensions;
using System;
using System.Collections;
using System.Linq;

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

        public static string Display(object target) {
            return target is null
                ? null
                : TargetDescription
                    .Describe(target.GetType())
                    .Display;
        }

        public static string Display(IEnumerable targets) {
            return targets is null
                ? null
                : string.Join(Environment.NewLine, targets
                    .OfType<object>()
                    .Select(Display));
        }

        public static string Manual(object target) {
            return target is null
                ? null
                : TargetDescription
                    .Describe(target.GetType())
                    .Manual;
        }

        public static string Manual(IEnumerable targets) {
            return targets is null
                ? null
                : string.Join(Environment.NewLine + Environment.NewLine, targets
                    .OfType<object>()
                    .Select(Manual));
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
