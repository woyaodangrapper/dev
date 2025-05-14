using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Contracts;

[StructLayout(LayoutKind.Explicit, Size = 138)]
public struct HeartHandles : IEquatable<HeartHandles>
{
    private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

    [FieldOffset(0)]
    public ushort Version;

    [FieldOffset(2)]
    public ulong Timestamp;

    [FieldOffset(10)]
    public ulong UserSecretsId;

    [FieldOffset(18)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
    public char[] HeartbeatName;

    public HeartHandles(byte[] bytes, Func<ReadOnlySpan<byte>, char[]>? decoder = null)
    {
        this = default;

        var span = bytes.AsSpan();
        Version = BitConverter.ToUInt16(span[..2]);             // 偏移量 0，占2字节
        Timestamp = BitConverter.ToUInt64(span.Slice(2, 8));           // 偏移量 2，占8字节
        UserSecretsId = BitConverter.ToUInt64(span.Slice(10, 8));      // 偏移量 10，占8字节

        HeartbeatName = (decoder ?? DefaultDecoder)(span.Slice(18, 60));// 偏移量 18，占60字节
    }

    public HeartHandles(ReadOnlySpan<byte> bytes, Func<ReadOnlySpan<byte>, char[]>? decoder = null)
    {
        this = default;
        Version = BitConverter.ToUInt16(bytes[..2]);             // 偏移量 0，占2字节
        Timestamp = BitConverter.ToUInt64(bytes.Slice(2, 8));           // 偏移量 2，占8字节
        UserSecretsId = BitConverter.ToUInt64(bytes.Slice(10, 8));      // 偏移量 10，占8字节
        HeartbeatName = (decoder ?? DefaultDecoder)(bytes.Slice(18, 60));    // 偏移量 18，占60字节
    }

    public HeartHandles(ReadOnlyMemory<byte> bytes, Func<ReadOnlySpan<byte>, char[]>? decoder = null)
    {
        this = default;
        ReadOnlySpan<byte> span = bytes.Span;
        Version = BitConverter.ToUInt16(span[..2]);             // 偏移量 0，占2字节
        Timestamp = BitConverter.ToUInt64(span.Slice(2, 8));           // 偏移量 2，占8字节
        UserSecretsId = BitConverter.ToUInt64(span.Slice(10, 8));      // 偏移量 10，占8字节
        HeartbeatName = (decoder ?? DefaultDecoder)(span.Slice(18, 60));     // 偏移量 18，占60字节
    }

    public HeartHandles(int? version = null, long? timestamp = null, long? userSecretsId = null, string? heartbeatName = null)
    {
        this = default;

        Version = (ushort)(version ?? 0);
        Timestamp = (ulong)(timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        UserSecretsId = (ulong)(userSecretsId ?? 0);
        HeartbeatName = [.. (heartbeatName ?? "").Take(60)];
    }

    /// <summary>
    /// 序列化为字节数组
    /// </summary>
    public readonly HeartHandles ToBytes(out byte[] buffer)
    {
        // (一个标准块大小，可能是 16、32、64……等)
        buffer = _bytePool.Rent(138);

        try
        {
            var span = buffer.AsSpan();
            BitConverter.TryWriteBytes(span[..2], Version);
            BitConverter.TryWriteBytes(span.Slice(2, 8), Timestamp);
            BitConverter.TryWriteBytes(span.Slice(10, 8), UserSecretsId);
            WriteCharToBytes(span.Slice(18, 60), HeartbeatName);
        }
        catch
        {
            // 确保在发生异常时返回字节数组给池中
            _bytePool.Return(buffer);
            throw;
        }
        return this;
    }

    public override readonly bool Equals(object? obj) // 修改为 object? 以匹配为 Null 性
    {
        return obj is HeartHandles other && Equals(other);
    }

    public readonly bool Equals(HeartHandles other)
    {
        return Version == other.Version &&
               Timestamp == other.Timestamp &&
               UserSecretsId == other.UserSecretsId &&
               HeartbeatName == other.HeartbeatName;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Version, Timestamp, UserSecretsId, HeartbeatName);
    }

    public static bool operator ==(HeartHandles left, HeartHandles right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HeartHandles left, HeartHandles right)
    {
        return !(left == right);
    }

    private static char[] DefaultDecoder(ReadOnlySpan<byte> bytes)
       => MemoryMarshal.Cast<byte, char>(bytes[..(bytes.Length & ~1)]).ToArray(); // 去掉最低位，确保是偶数

    public static bool WriteCharToBytes(Span<byte> destination, char[] value, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        byte[] bytes = encoding.GetBytes(value);

        if (destination.Length < bytes.Length)
        {
            return false;
        }
        bytes.CopyTo(destination);
        return true;
    }
}