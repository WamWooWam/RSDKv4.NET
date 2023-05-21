using System;
namespace RSDKv4.Utility;

public class ArraySlice<T> 
{
    private readonly T[] array;
    private readonly int offset;
    private readonly int count;

    public ArraySlice(T[] array, int offset, int count)
    {
        this.array = array;
        this.offset = offset;
        this.count = count;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= count)
            {
                throw new IndexOutOfRangeException();
            }

            return array[offset + index];
        }
        set
        {
            if (index < 0 || index >= count)
            {
                throw new IndexOutOfRangeException();
            }

            array[offset + index] = value;
        }
    }
}
