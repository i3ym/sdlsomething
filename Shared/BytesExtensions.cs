namespace SdlSomethingShared;

public static class BytesExtensions
{
    public static int EnsureArrayLength(int min, int current, int target)
    {
        current = Math.Max(current, min);
        while (current < target)
            current *= 2;

        return current;
    }
    public static T EnsureArrayLength<T>(T min, T current, T target)
        where T : INumber<T>
    {
        current = T.Max(current, min);
        while (current < target)
            current *= T.CreateChecked(2);

        return current;
    }
}
