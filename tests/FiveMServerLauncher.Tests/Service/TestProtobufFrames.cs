using Google.Protobuf;

namespace FiveMServerLauncher.Tests.Service;

internal static class TestProtobufFrames
{
    public static byte[] BuildFrameStream(Master.Server server)
    {
        var payload = server.ToByteArray();
        var frame = new byte[4 + payload.Length];
        BitConverter.GetBytes(payload.Length).CopyTo(frame, 0);
        payload.CopyTo(frame, 4);
        return frame;
    }

    public static byte[] Join(params byte[][] chunks)
    {
        var result = new byte[chunks.Sum(c => c.Length)];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            chunk.CopyTo(result, offset);
            offset += chunk.Length;
        }

        return result;
    }
}