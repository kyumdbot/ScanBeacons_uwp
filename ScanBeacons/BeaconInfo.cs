using System;
using System.Linq;

namespace ScanBeacons
{
    public struct BeaconInfo
    {
        public Guid Uuid { get; }
        public ushort Major { get; }
        public ushort Minor { get; }
        public sbyte TxPower { get; }
        public short Rssi { get; }

        public BeaconInfo(Guid uuid, ushort major, ushort minor, sbyte txPower, short rssi)
        {
            Uuid = uuid;
            Major = major;
            Minor = minor;
            TxPower = txPower;
            Rssi = rssi;
        }

        public static BeaconInfo Create(byte[] data, short rssi)
        {
            if (data[0] != 0x02)
            {
                throw new ArgumentException("First byte in array was exptected to be 0x02", "bytes");
            }
            if (data[1] != 0x15)
            {
                throw new ArgumentException("Second byte in array was expected to be 0x15", "bytes");
            }
            if (data.Length != 23)
            {
                throw new ArgumentException("Byte array length was expected to be 23", "bytes");
            }

            var uuid = new Guid(
                        BitConverter.ToInt32(data.Skip(2).Take(4).Reverse().ToArray(), 0),
                        BitConverter.ToInt16(data.Skip(6).Take(2).Reverse().ToArray(), 0),
                        BitConverter.ToInt16(data.Skip(8).Take(2).Reverse().ToArray(), 0),
                        data.Skip(10).Take(8).ToArray());

            ushort major = BitConverter.ToUInt16(data.Skip(18).Take(2).Reverse().ToArray(), 0);
            ushort minor = BitConverter.ToUInt16(data.Skip(20).Take(2).Reverse().ToArray(), 0);
            sbyte txPower = (sbyte)data[22];

            return new BeaconInfo(uuid, major, minor, txPower, rssi);
        }

        public override string ToString()
        {
            return $"Uuid: {Uuid}, Major: {Major}, Minor: {Minor}, TxPower: {TxPower}, Rssi: {Rssi}";
        }
    }
}
