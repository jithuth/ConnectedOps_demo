using System.Buffers.Binary;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Telematics;

namespace ConnectedOps.Infrastructure.Telematics.Providers;

public static class TeltonikaCodec8Parser
{
    public const byte Codec8 = 0x08;
    public const byte Codec8Extended = 0x8E;

    public static IReadOnlyList<NormalizedTelemetryMessage> Parse(
        byte[] payload,
        string? deviceIdentifier = null)
    {
        if (payload == null || payload.Length < 15)
        {
            return [];
        }

        var span = payload.AsSpan();
        int offset = 0;

        // Check if there is a 4-byte zero preamble (TCP standard: 0x00000000)
        if (span.Length >= 4 && BinaryPrimitives.ReadInt32BigEndian(span) == 0)
        {
            offset += 4;
            if (offset + 4 > span.Length)
                return [];

            int dataLength = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
            offset += 4;
        }

        if (offset >= span.Length)
            return [];

        byte codecId = span[offset++];
        if (codecId != Codec8 && codecId != Codec8Extended)
        {
            return [];
        }

        if (offset >= span.Length)
            return [];

        int recordCount = span[offset++];
        var results = new List<NormalizedTelemetryMessage>(recordCount);
        var receivedAtUtc = DateTime.UtcNow;

        for (int i = 0; i < recordCount && offset < span.Length - 1; i++)
        {
            if (offset + 8 > span.Length) break;
            long timestampMs = BinaryPrimitives.ReadInt64BigEndian(span.Slice(offset, 8));
            offset += 8;
            var recordedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime;

            if (offset >= span.Length) break;
            byte priority = span[offset++];

            // GPS Element (15 bytes)
            if (offset + 15 > span.Length) break;
            int lonRaw = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
            offset += 4;
            int latRaw = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
            offset += 4;
            short altitude = BinaryPrimitives.ReadInt16BigEndian(span.Slice(offset, 2));
            offset += 2;
            ushort angle = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
            offset += 2;
            byte satellites = span[offset++];
            ushort speed = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
            offset += 2;

            double longitude = lonRaw / 10000000.0;
            double latitude = latRaw / 10000000.0;

            // IO Elements
            var ioElements = new Dictionary<int, object>();
            int eventIoId = 0;

            if (codecId == Codec8)
            {
                if (offset >= span.Length) break;
                eventIoId = span[offset++];
                if (offset >= span.Length) break;
                int totalIo = span[offset++];

                // 1-byte elements
                if (offset >= span.Length) break;
                int count1 = span[offset++];
                for (int j = 0; j < count1 && offset + 2 <= span.Length; j++)
                {
                    byte id = span[offset++];
                    byte val = span[offset++];
                    ioElements[id] = val;
                }

                // 2-byte elements
                if (offset >= span.Length) break;
                int count2 = span[offset++];
                for (int j = 0; j < count2 && offset + 3 <= span.Length; j++)
                {
                    byte id = span[offset++];
                    ushort val = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                    offset += 2;
                    ioElements[id] = val;
                }

                // 4-byte elements
                if (offset >= span.Length) break;
                int count4 = span[offset++];
                for (int j = 0; j < count4 && offset + 5 <= span.Length; j++)
                {
                    byte id = span[offset++];
                    uint val = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4));
                    offset += 4;
                    ioElements[id] = val;
                }

                // 8-byte elements
                if (offset >= span.Length) break;
                int count8 = span[offset++];
                for (int j = 0; j < count8 && offset + 9 <= span.Length; j++)
                {
                    byte id = span[offset++];
                    ulong val = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(offset, 8));
                    offset += 8;
                    ioElements[id] = val;
                }
            }
            else // Codec8Extended
            {
                if (offset + 4 > span.Length) break;
                eventIoId = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                offset += 2;
                int totalIo = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                offset += 2;

                // 1-byte elements
                if (offset + 2 > span.Length) break;
                int count1 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                offset += 2;
                for (int j = 0; j < count1 && offset + 3 <= span.Length; j++)
                {
                    ushort id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                    offset += 2;
                    byte val = span[offset++];
                    ioElements[id] = val;
                }

                // 2-byte elements
                if (offset + 2 > span.Length) break;
                int count2 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                offset += 2;
                for (int j = 0; j < count2 && offset + 4 <= span.Length; j++)
                {
                    ushort id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                    offset += 2;
                    ushort val = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                    offset += 2;
                    ioElements[id] = val;
                }

                // 4-byte elements
                if (offset + 2 > span.Length) break;
                int count4 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                offset += 2;
                for (int j = 0; j < count4 && offset + 6 <= span.Length; j++)
                {
                    ushort id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                    offset += 2;
                    uint val = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4));
                    offset += 4;
                    ioElements[id] = val;
                }

                // 8-byte elements
                if (offset + 2 > span.Length) break;
                int count8 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                offset += 2;
                for (int j = 0; j < count8 && offset + 10 <= span.Length; j++)
                {
                    ushort id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                    offset += 2;
                    ulong val = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(offset, 8));
                    offset += 8;
                    ioElements[id] = val;
                }
            }

            // Normalization of common Teltonika IO properties
            bool? ignition = null;
            if (ioElements.TryGetValue(239, out var ignVal))
            {
                ignition = Convert.ToInt64(ignVal) != 0;
            }

            decimal? extPowerVoltage = null;
            if (ioElements.TryGetValue(66, out var extVVal))
            {
                extPowerVoltage = Convert.ToDecimal(extVVal) / 1000m;
            }

            decimal? batteryVoltage = null;
            if (ioElements.TryGetValue(67, out var batVVal))
            {
                batteryVoltage = Convert.ToDecimal(batVVal) / 1000m;
            }

            int? batteryPct = null;
            if (ioElements.TryGetValue(113, out var batPVal))
            {
                batteryPct = Convert.ToInt32(batPVal);
            }

            int? gsmSignal = null;
            if (ioElements.TryGetValue(21, out var gsmVal))
            {
                gsmSignal = Convert.ToInt32(gsmVal);
            }

            decimal? odometerKm = null;
            if (ioElements.TryGetValue(16, out var odoVal))
            {
                odometerKm = Convert.ToDecimal(odoVal) / 1000m; // Meters to KM
            }

            decimal? engineHours = null;
            if (ioElements.TryGetValue(248, out var engVal))
            {
                engineHours = Convert.ToDecimal(engVal) / 3600m; // Seconds to hours
            }

            bool? di1 = null;
            if (ioElements.TryGetValue(1, out var di1Val))
            {
                di1 = Convert.ToInt64(di1Val) != 0;
            }

            bool? di2 = null;
            if (ioElements.TryGetValue(2, out var di2Val))
            {
                di2 = Convert.ToInt64(di2Val) != 0;
            }

            var eventType = TelemetryEventType.Periodic;
            if (ignition.HasValue)
            {
                eventType = ignition.Value ? TelemetryEventType.IgnitionOn : TelemetryEventType.IgnitionOff;
            }

            var normalized = new NormalizedTelemetryMessage(
                DeviceIdentifier: deviceIdentifier ?? "TELTONIKA_DEVICE",
                Provider: "Teltonika",
                RecordedAtUtc: recordedAtUtc,
                ReceivedAtUtc: receivedAtUtc,
                Latitude: latitude,
                Longitude: longitude,
                AltitudeMeters: altitude,
                SpeedKph: speed,
                HeadingDegrees: angle,
                IgnitionOn: ignition,
                OdometerKm: odometerKm,
                EngineHours: engineHours,
                BatteryVoltage: batteryVoltage,
                ExternalPowerVoltage: extPowerVoltage,
                GsmSignal: gsmSignal,
                GpsSatellites: satellites,
                DigitalInput1: di1,
                DigitalInput2: di2,
                RawEventCode: eventIoId,
                EventType: eventType,
                AdditionalIoElements: ioElements);

            results.Add(normalized);
        }

        return results;
    }
}
