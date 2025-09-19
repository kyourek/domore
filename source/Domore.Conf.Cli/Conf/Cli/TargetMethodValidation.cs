using System;
using System.Reflection;

namespace Domore.Conf.Cli; 
internal sealed class TargetMethodValidation {
    public MethodInfo MethodInfo { get; }
    public CliValidationAttribute Attribute { get; }

    public TargetMethodValidation(MethodInfo methodInfo, CliValidationAttribute attribute) {
        MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
        Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
    }

    public void Run(object target) {
        var result = MethodInfo.Invoke(target, null);
        var passed = result as bool?;
        if (passed.HasValue) {
            if (passed.Value == false) {
                throw new CliValidationException(Attribute.Message);
            }
        }
    }
}
