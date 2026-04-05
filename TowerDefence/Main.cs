namespace SdlSomething.TowerDefence;

public sealed class Main
{
    public Main()
    {
        
    }
}

public sealed class PlacementField
{

}


// public sealed class StuffBuffer<T>
// {
//     T[] Items = [];
//     int Length = 0;

//     public int Add(ref T item)
//     {
//         if (Items.Length < Length + 1)
//             Array.Resize(ref Items, Math.Max(Items.Length, 64) * 2);

//         var idx = Length;
//         Length++;
//         Items[idx] = item;
//         return idx;
//     }
//     public void Remove(int index)
//     {
//         if (Items.Length < Length + 1)
//             Array.Resize(ref Items, Math.Max(Items.Length, 64) * 2);

//         var idx = Length;
//         Length++;
//         Items[idx] = item;
//         return idx;
//     }
// }

public abstract class TowerGroup
{

}
public sealed class BasicTowerGroup : TowerGroup
{

}
