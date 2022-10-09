using System;
#if SILVERLIGHT
using System.IO.IsolatedStorage;
#endif

namespace RSDKv4.Utility;
public class ArraySlice<T>
{
    private T[] array;
    private int offset;
    private int count;

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
            if (index >= count)
            {
                throw new IndexOutOfRangeException();
            }

            return array[offset + index];
        }
        set
        {
            if (index >= count)
            {
                throw new IndexOutOfRangeException();
            }

            array[offset + index] = value;
        }
    }
}
