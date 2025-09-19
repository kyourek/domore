using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
#if !NET40
using System.Runtime.CompilerServices;
#endif

namespace Domore.Notification; 
public partial class Notifier {
    protected internal bool Change(
        ref bool field,
        bool value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref bool field,
        bool value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref bool field, bool value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref bool field, bool value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref bool? field,
        bool? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref bool? field,
        bool? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref bool? field, bool? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref bool? field, bool? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref byte field,
        byte value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref byte field,
        byte value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref byte field, byte value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref byte field, byte value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref byte? field,
        byte? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref byte? field,
        byte? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref byte? field, byte? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref byte? field, byte? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref sbyte field,
        sbyte value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref sbyte field,
        sbyte value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref sbyte field, sbyte value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref sbyte field, sbyte value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref sbyte? field,
        sbyte? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref sbyte? field,
        sbyte? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref sbyte? field, sbyte? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref sbyte? field, sbyte? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref char field,
        char value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref char field,
        char value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref char field, char value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref char field, char value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref char? field,
        char? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref char? field,
        char? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref char? field, char? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref char? field, char? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref decimal field,
        decimal value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref decimal field,
        decimal value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref decimal field, decimal value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref decimal field, decimal value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref decimal? field,
        decimal? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref decimal? field,
        decimal? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref decimal? field, decimal? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref decimal? field, decimal? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref double field,
        double value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref double field,
        double value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref double field, double value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref double field, double value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref double? field,
        double? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref double? field,
        double? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref double? field, double? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref double? field, double? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref float field,
        float value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref float field,
        float value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref float field, float value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref float field, float value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref float? field,
        float? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref float? field,
        float? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref float? field, float? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref float? field, float? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref int field,
        int value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref int field,
        int value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref int field, int value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref int field, int value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref int? field,
        int? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref int? field,
        int? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref int? field, int? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref int? field, int? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref uint field,
        uint value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref uint field,
        uint value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref uint field, uint value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref uint field, uint value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref uint? field,
        uint? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref uint? field,
        uint? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref uint? field, uint? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref uint? field, uint? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref nint field,
        nint value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref nint field,
        nint value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref nint field, nint value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref nint field, nint value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref nint? field,
        nint? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref nint? field,
        nint? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref nint? field, nint? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref nint? field, nint? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref nuint field,
        nuint value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref nuint field,
        nuint value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref nuint field, nuint value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref nuint field, nuint value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref nuint? field,
        nuint? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref nuint? field,
        nuint? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref nuint? field, nuint? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref nuint? field, nuint? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref long field,
        long value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref long field,
        long value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref long field, long value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref long field, long value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref long? field,
        long? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref long? field,
        long? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref long? field, long? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref long? field, long? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref ulong field,
        ulong value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref ulong field,
        ulong value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref ulong field, ulong value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref ulong field, ulong value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref ulong? field,
        ulong? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref ulong? field,
        ulong? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref ulong? field, ulong? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref ulong? field, ulong? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref short field,
        short value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref short field,
        short value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref short field, short value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref short field, short value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref short? field,
        short? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref short? field,
        short? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref short? field, short? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref short? field, short? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref ushort field,
        ushort value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref ushort field,
        ushort value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref ushort field, ushort value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref ushort field, ushort value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref ushort? field,
        ushort? value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref ushort? field,
        ushort? value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (field == value) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref ushort? field, ushort? value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref ushort? field, ushort? value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (field == value) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change(
        ref string field,
        string value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (string.Equals(field, value)) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change(
        ref string field,
        string value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (string.Equals(field, value)) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change(ref string field, string value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (string.Equals(field, value)) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change(ref string field, string value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (string.Equals(field, value)) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
    protected internal bool Change<T>(
        ref T field,
        T value,
        #if NET40
            string propertyName
        #else
            [CallerMemberName] string propertyName = null
        #endif
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        NotifyPropertyChanging(propertyName);
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
    
    protected internal bool Change<T>(
        ref T field,
        T value,
        #if NET40
            string propertyName,
        #else
            [CallerMemberName] string propertyName = null,
        #endif
        params string[] dependentPropertyNames
        ) {
        if (PreviewPropertyChange(propertyName) == false) return false;
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        NotifyPropertyChanging(propertyName, dependentPropertyNames);
        field = value;
        NotifyPropertyChanged(propertyName, dependentPropertyNames);
        return true;
    }
    
    protected internal bool Change<T>(ref T field, T value, PropertyChangedEventArgs e) {
        if (PreviewPropertyChange(e) == false) return false;
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName));
        field = value;
        NotifyPropertyChanged(e);
        return true;
    }
    
    protected internal bool Change<T>(ref T field, T value, PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
        if (PreviewPropertyChange(e) == false) return false;
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        if (PropertyChanging != null) NotifyPropertyChanging(new PropertyChangingEventArgs(e?.PropertyName), dependents?.Select(d => new PropertyChangingEventArgs(d?.PropertyName)).ToArray());
        field = value;
        NotifyPropertyChanged(e, dependents);
        return true;
    }
}
