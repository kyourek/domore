using Domore.Conf.Extensions;
using System;
using System.Collections;
using System.Linq;
using System.Threading;

namespace Domore.Conf.Cli; 
public sealed class CliProvider {
    private readonly Lazy<TargetDescription.Cache> LazyTargetDescription;

    private TargetDescription.Cache TargetDescription => _TargetDescription ??= LazyTargetDescription.Value;
    private TargetDescription.Cache _TargetDescription;

    public CliSetup Setup { get; }

    public CliProvider(CliSetup setup) {
        Setup = setup;
        LazyTargetDescription = new(() => new(Setup), LazyThreadSafetyMode.PublicationOnly);
    }

    private T Validate<T>(T target) {
        if (target is null) {
            throw new ArgumentNullException(nameof(target));
        }
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
            throw new CliValidationException($"Invalid command: {ex?.Message}", ex);
        }
        return target;
    }

    private T Conf<T>(T target, string line) {
        if (target is null) {
            throw new ArgumentNullException(nameof(target));
        }
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

    public T Configure<T>(T target, string line) {
        return Validate(Conf(target, line));
    }

    public string Display(object target) {
        return target is null
            ? null
            : TargetDescription
                .Describe(target.GetType())
                .Display;
    }

    public string Display(IEnumerable targets) {
        return targets is null
            ? null
            : string.Join(Environment.NewLine, targets
                .OfType<object>()
                .Select(Display));
    }

    public string Manual(object target) {
        return target is null
            ? null
            : TargetDescription
                .Describe(target.GetType())
                .Manual;
    }

    public string Manual(IEnumerable targets) {
        return targets is null
            ? null
            : string.Join(Environment.NewLine + Environment.NewLine, targets
                .OfType<object>()
                .Select(Manual));
    }
}
