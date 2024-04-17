using Domore.Conf.Extensions;
using System;
using System.Linq;

namespace Domore.Conf.Cli {
    public static class Cli {
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

        public static T Configure<T>(T target, string line) {
            return Validate(Conf(target, line));
        }

        public static string Display<T>(T target, CliDisplayOptions options) {
            if (null == target) throw new ArgumentNullException(nameof(target));
            var description = TargetDescription.Describe(target.GetType());
            var display = description.Display;
            display = options.HasFlag(CliDisplayOptions.SkipCommandName)
                ? string.Join(" ", display.Split(' ').Skip(1))
                : display;
            return display;
        }

        public static string Display<T>(T target) {
            return Display(target, CliDisplayOptions.None);
        }
    }
}
