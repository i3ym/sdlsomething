namespace SdlSomethingShared;

public static class BytesExtensions
{
    public static bool EnsureArrayLength<T>(ref T[] arr, int min, int target)
    {
        if (arr.Length >= target) return false;

        var prev = arr;
        arr = new T[BytesExtensions.EnsureArrayLength(min, arr.Length, target)];
        prev.CopyTo(arr);
        return true;
    }

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
