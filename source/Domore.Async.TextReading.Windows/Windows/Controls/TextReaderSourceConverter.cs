using Domore.IO;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Domore.Windows.Controls;

internal sealed class TextReaderSourceConverter : IValueConverter {
    object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is IStreamText) {
            return value;
        }
        if (value is string s) {
            if (!string.IsNullOrEmpty(s)) {
                if (!Path.GetInvalidPathChars().Any(c => s.Contains(c))) {
                    var fileInfo = new FileInfo(s);
                    if (fileInfo.Exists) {
                        return new StreamTextSourceFile(fileInfo);
                    }
                }
            }
        }
        return DependencyProperty.UnsetValue;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}
