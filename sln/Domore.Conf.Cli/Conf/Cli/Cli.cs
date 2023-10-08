using Domore.Conf.Extensions;
using System;

namespace Domore.Conf.Cli {
    public static class Cli {
        private static T Validate<T>(T target) {
            var description = TargetDescription.Describe(typeof(T));
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
            var description = TargetDescription.Describe(typeof(T));
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

        public static string Display<T>(T target) {
            var description = TargetDescription.Describe(typeof(T));
            return description.Display;
        }
    }
}
