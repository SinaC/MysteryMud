using System.Buffers;

namespace MysteryMud.Infrastructure.Network;

public class OutputBuffer : IDisposable
{
    private byte[] _buffer;
    private int _length;

    public OutputBuffer(int initialSize = 1024)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialSize);
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);

        data.CopyTo(_buffer.AsSpan(_length));
        _length += data.Length;
    }

    public void WriteByte(byte b)
    {
        EnsureCapacity(1);
        _buffer[_length++] = b;
    }

    public ReadOnlyMemory<byte> Data => _buffer.AsMemory(0, _length);
    //public ReadOnlySpan<byte> AsSpan() => _buffer.AsSpan(0, _length);  use if asynchronous API is needed

    public void Clear() => _length = 0;

    private void EnsureCapacity(int additional)
    {
        if (_length + additional <= _buffer.Length)
            return;

        int newSize = Math.Max(_buffer.Length * 2, _length + additional);

        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);

        _buffer.AsSpan(0, _length).CopyTo(newBuffer);

        ArrayPool<byte>.Shared.Return(_buffer);

        _buffer = newBuffer;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = Array.Empty<byte>();
        _length = 0;
    }
}
