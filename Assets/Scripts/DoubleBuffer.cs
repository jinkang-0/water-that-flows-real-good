using UnityEngine;

public class DoubleBuffer<T>
{
    public ComputeBuffer bufferRead;
    public ComputeBuffer bufferWrite;
    private int length;

    public DoubleBuffer(int count)
    {
        bufferRead = ComputeHelper.CreateStructuredBuffer<T>(count);
        bufferWrite = ComputeHelper.CreateStructuredBuffer<T>(count);
        length = count;
    }

    public void Swap()
    {
        (bufferRead, bufferWrite) = (bufferWrite, bufferRead);
    }

    public void SyncToWrite()
    {
        T[] arr = new T[length];
        bufferRead.GetData(arr);
        bufferWrite.SetData(arr);
    }

    public void Destroy()
    {
        bufferRead.Release();
        bufferWrite.Release();
    }
}
