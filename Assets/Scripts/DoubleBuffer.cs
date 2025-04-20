using UnityEngine;

public class DoubleBuffer<T>
{
    public ComputeBuffer bufferRead;
    public ComputeBuffer bufferWrite;

    public DoubleBuffer(int num_elems)
    {
        bufferRead = ComputeHelper.CreateStructuredBuffer<T>(num_elems);
        bufferWrite = ComputeHelper.CreateStructuredBuffer<T>(num_elems);
    }

    public void Swap()
    {
        (bufferRead, bufferWrite) = (bufferWrite, bufferRead);
    }

    public void Destroy()
    {
        bufferRead.Release();
        bufferWrite.Release();
    }
}
