using System.Globalization;

namespace SdlSomething;

public readonly partial struct Fixed3
{
    public static Fixed3 One => new(Scale);
    public static Fixed3 Zero => default;


    const long Scale = 1000;
    const long HalfScale = Scale / 2;
    readonly long RawValue;

    public Fixed3(long rawValue) => RawValue = rawValue;

    public static Fixed3 From(int value) => new Fixed3(value * Scale);
    public static Fixed3 From(float value) => new Fixed3((long) MathF.Round(value * Scale, MidpointRounding.AwayFromZero));
    public static Fixed3 From(double value) => new Fixed3((long) Math.Round(value * Scale, MidpointRounding.AwayFromZero));

    public int ToInt() => (int) (RawValue / Scale);
    public float ToFloat() => (float) RawValue / Scale;
    public double ToDouble() => (double) RawValue / Scale;

    public static Fixed3 operator +(Fixed3 a, int b) => a + From(b);
    public static Fixed3 operator -(Fixed3 a, int b) => a - From(b);
    public static Fixed3 operator *(Fixed3 a, int b) => a * From(b);
    public static Fixed3 operator /(Fixed3 a, int b) => a / From(b);

    public static Fixed3 operator +(Fixed3 a, Fixed3 b) => new Fixed3(a.RawValue + b.RawValue);
    public static Fixed3 operator -(Fixed3 a, Fixed3 b) => new Fixed3(a.RawValue - b.RawValue);

    public static Fixed3 operator *(Fixed3 a, Fixed3 b)
    {
        var result = a.RawValue * b.RawValue;

        if (result >= 0)
            return new Fixed3((result + HalfScale) / Scale);
        else return new Fixed3((result - HalfScale) / Scale);
    }
    public static Fixed3 operator /(Fixed3 a, Fixed3 b)
    {
        if (b.RawValue == 0)
            throw new DivideByZeroException();

        var numerator = a.RawValue * Scale;
        if ((numerator ^ b.RawValue) >= 0)
            return new Fixed3((numerator + (b.RawValue / 2)) / b.RawValue);
        else return new Fixed3((numerator - (b.RawValue / 2)) / b.RawValue);
    }
    public static Fixed3 operator %(Fixed3 left, Fixed3 right) => new(left.RawValue % right.RawValue);

    public static Fixed3 operator ++(Fixed3 a) => a + One;
    public static Fixed3 operator --(Fixed3 a) => a - One;
    public static Fixed3 operator +(Fixed3 a) => a;
    public static Fixed3 operator -(Fixed3 a) => new(-a.RawValue);
    public static bool operator ==(Fixed3 a, Fixed3 b) => a.RawValue == b.RawValue;
    public static bool operator !=(Fixed3 a, Fixed3 b) => a.RawValue != b.RawValue;
    public static bool operator >(Fixed3 a, Fixed3 b) => a.RawValue > b.RawValue;
    public static bool operator <(Fixed3 a, Fixed3 b) => a.RawValue < b.RawValue;
    public static bool operator >=(Fixed3 a, Fixed3 b) => a.RawValue >= b.RawValue;
    public static bool operator <=(Fixed3 a, Fixed3 b) => a.RawValue <= b.RawValue;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Fixed3 other && Equals(other);
    public bool Equals(Fixed3 other) => RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();

    public override string ToString() => ToString(CultureInfo.InvariantCulture);
    public string ToString(CultureInfo culture) => ToDouble().ToString(culture);

    public static implicit operator Fixed3(int value) => From(value);
}

public readonly partial struct Fixed3
    : INumber<Fixed3>
{
    public static Fixed3 MaxValue => new(long.MaxValue);
    public static Fixed3 MinValue => new(long.MinValue);
    static int INumberBase<Fixed3>.Radix => 10;
    static Fixed3 IAdditiveIdentity<Fixed3, Fixed3>.AdditiveIdentity => Zero;
    static Fixed3 IMultiplicativeIdentity<Fixed3, Fixed3>.MultiplicativeIdentity => One;


    int IComparable.CompareTo(object? obj) => CompareTo((Fixed3) obj!);
    public int CompareTo(Fixed3 other) => RawValue.CompareTo(other.RawValue);

    public static int Sign(Fixed3 x) => Math.Sign(x.RawValue);
    public static Fixed3 Sqrt(Fixed3 value)
    {
        if (value.RawValue < 0) throw new ArgumentOutOfRangeException();
        if (value.RawValue == 0) return Zero;

        var n = value.RawValue * Scale;
        var x = n;
        var y = 1L;

        while (x > y)
        {
            x = (x + y) / 2;
            y = n / x;
        }
        return new Fixed3(x);
    }

    public static Fixed3 VecMagnitude(Fixed3 fx, Fixed3 fy)
    {
        var x = (double) fx.RawValue / Fixed3.Scale;
        var y = (double) fy.RawValue / Fixed3.Scale;
        var mag = Math.Sqrt(x * x + y * y);

        return From(mag);
    }
    public static void VecNormalize(ref Fixed3 fx, ref Fixed3 fy)
    {
        var mag = VecMagnitude(fx, fy);

        if (mag == Fixed3.Zero)
        {
            fx = fy = Zero;
            return;
        }

        fx /= mag;
        fy /= mag;
    }
    public static Fixed3 Abs(Fixed3 value) => new(Math.Abs(value.RawValue));
    public static bool IsCanonical(Fixed3 value) => true;
    public static bool IsComplexNumber(Fixed3 value) => false;
    public static bool IsEvenInteger(Fixed3 value) => (value.RawValue / Scale) % 2 == 0;
    public static bool IsOddInteger(Fixed3 value) => (value.RawValue / Scale) % 2 != 0;
    public static bool IsFinite(Fixed3 value) => true;
    public static bool IsImaginaryNumber(Fixed3 value) => false;
    public static bool IsInfinity(Fixed3 value) => false;
    public static bool IsInteger(Fixed3 value) => value.RawValue % Scale == 0;
    public static bool IsNaN(Fixed3 value) => false;
    public static bool IsNegative(Fixed3 value) => value.RawValue < 0;
    public static bool IsNegativeInfinity(Fixed3 value) => false;
    public static bool IsNormal(Fixed3 value) => value.RawValue != 0;
    public static bool IsPositive(Fixed3 value) => value.RawValue > 0;
    public static bool IsPositiveInfinity(Fixed3 value) => false;
    public static bool IsRealNumber(Fixed3 value) => true;
    public static bool IsSubnormal(Fixed3 value) => false;
    public static bool IsZero(Fixed3 value) => value.RawValue == 0;
    public static Fixed3 MaxMagnitude(Fixed3 x, Fixed3 y) => Abs(x) > Abs(y) ? x : y;
    public static Fixed3 MinMagnitude(Fixed3 x, Fixed3 y) => Abs(x) < Abs(y) ? x : y;
    public static Fixed3 MaxMagnitudeNumber(Fixed3 x, Fixed3 y) => MaxMagnitude(x, y);
    public static Fixed3 MinMagnitudeNumber(Fixed3 x, Fixed3 y) => MinMagnitude(x, y);

    public static Fixed3 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => From(double.Parse(s, style, provider));
    public static Fixed3 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => From(double.Parse(s, provider));
    public static Fixed3 Parse(string s, NumberStyles style, IFormatProvider? provider) => From(double.Parse(s, style, provider));
    public static Fixed3 Parse(string s, IFormatProvider? provider) => From(double.Parse(s, provider));

    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Fixed3 result)
    {
        if (double.TryParse(s, style, provider, out var res))
        {
            result = From(res);
            return true;
        }

        result = default;
        return false;
    }
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Fixed3 result)
    {
        if (double.TryParse(s, provider, out var res))
        {
            result = From(res);
            return true;
        }

        result = default;
        return false;
    }
    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Fixed3 result)
    {
        if (double.TryParse(s, style, provider, out var res))
        {
            result = From(res);
            return true;
        }

        result = default;
        return false;
    }
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Fixed3 result)
    {
        if (double.TryParse(s, provider, out var res))
        {
            result = From(res);
            return true;
        }

        result = default;
        return false;
    }

    public static bool TryConvertFromChecked<TOther>(TOther value, out Fixed3 result) where TOther : INumberBase<TOther>
    {
        result = From(double.CreateChecked(value));
        return true;
    }
    public static bool TryConvertFromSaturating<TOther>(TOther value, out Fixed3 result) where TOther : INumberBase<TOther>
    {
        result = From(double.CreateSaturating(value));
        return true;
    }
    public static bool TryConvertFromTruncating<TOther>(TOther value, out Fixed3 result) where TOther : INumberBase<TOther>
    {
        result = From(double.CreateTruncating(value));
        return true;
    }

    public static bool TryConvertToChecked<TOther>(Fixed3 value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther> =>
        TOther.TryConvertFromChecked(value.ToDouble(), out result);
    public static bool TryConvertToSaturating<TOther>(Fixed3 value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther> =>
        TOther.TryConvertFromSaturating(value.ToDouble(), out result);
    public static bool TryConvertToTruncating<TOther>(Fixed3 value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther> =>
        TOther.TryConvertFromTruncating(value.ToDouble(), out result);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        ToDouble().TryFormat(destination, out charsWritten, format, provider);
    public string ToString(string? format, IFormatProvider? formatProvider) => ToDouble().ToString(format, formatProvider);
}
