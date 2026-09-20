using Google.Protobuf;

namespace FiveMServerLauncher.Service;

public static class ServerCatalogDecoder
{
    public static IReadOnlyList<Master.Server> Decode(byte[] data)
    {
        var servers = new List<Master.Server>();
        var offset = 0;

        while (offset < data.Length)
        {
            if (offset + sizeof(uint) > data.Length)
            {
                return servers; // truncated frame at the end
            }

            var length = BitConverter.ToUInt32(data, offset);
            offset += sizeof(uint);

            if (offset + length > data.Length)
            {
                return servers; // truncated frame at the end
            }

            try
            {
                servers.Add(Master.Server.Parser.ParseFrom(data, offset, (int)length));
            }
            catch (InvalidProtocolBufferException)
            {
                // corrupt frame: skip it and keep decoding the rest
            }

            offset += (int)length;
        }

        return servers;
    }
}