using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleBufferHelper<T>
{
    // the buffer that is to be read from
    public ComputeBuffer buffer_read { get; private set; }
    // the buffer that is to be written to
    public ComputeBuffer buffer_write { get; private set; }

    public DoubleBufferHelper(int element_count)
    {
        buffer_read = new ComputeBuffer(element_count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)));
        buffer_write = new ComputeBuffer(element_count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)));
    }
    public void swap()
    {
        (buffer_read, buffer_write) = (buffer_write, buffer_read);
    }
    public void destroy()
    {
        buffer_read.Release();
        buffer_write.Release();
    }
}
