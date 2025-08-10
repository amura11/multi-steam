using System;
using System.Windows.Markup;

namespace PlayniteMultiAccountSteamLibrary.Extension.Infrastructure;

public class EnumValuesExtension : MarkupExtension
{
    public Type EnumType { get; set; }

    public EnumValuesExtension() { }

    public EnumValuesExtension(Type enumType)
    {
        this.EnumType = enumType;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (this.EnumType == null || !this.EnumType.IsEnum)
        {
            throw new InvalidOperationException("EnumValuesExtension requires a valid EnumType.");
        }

        return Enum.GetValues(this.EnumType);
    }
}