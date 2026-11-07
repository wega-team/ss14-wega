using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Random;

namespace Content.Shared.Genetics.Systems;

public abstract partial class SharedDnaModifierSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private IRobustRandom _random = default!;

    public void TrySaveInDisk(EntityUid disk, EnzymeInfo enzyme)
    {
        if (!TryComp(disk, out DnaModifierDiskComponent? comp))
            return;

        if (comp.Data != null)
            return;

        comp.Data = (EnzymeInfo)enzyme.Clone();
        _metaData.SetEntityName(disk, Loc.GetString("dna-disk-name")+ " " + $"({enzyme.SampleName})");

        Dirty(disk, comp);
    }

    public bool TryGetDataFromDisk(EntityUid disk, [NotNullWhen(true)] out EnzymeInfo? data)
    {
        data = null;
        if (!TryComp(disk, out DnaModifierDiskComponent? comp))
            return false;

        if (comp.Data == null)
            return false;

        data = comp.Data;
        return true;
    }

    public bool TryClearDiskData(EntityUid disk)
    {
        if (!TryComp(disk, out DnaModifierDiskComponent? comp))
            return false;

        if (comp.Data == null)
            return false;

        comp.Data = null;
        return true;
    }

    public static Color ToColor(uint color)
        => new((byte)(color >> 16), (byte)(color >> 8), (byte)color);

    public static uint FromColor(Color color)
        => (uint)((byte)(color.R * 255) << 16 | (byte)(color.G * 255) << 8 | (byte)(color.B * 255));

    public uint GenerateRandomColor() => (uint)_random.Next(0x1000000);

    public uint GenerateRandomSkinTone() => (uint)_random.Next(0, 101);

    public uint MutateColorDigit(uint color, int digitIndex, float intensity)
    {
        var hex = color.ToString("X6");
        if (digitIndex < 0 || digitIndex >= 6)
            return color;

        var chars = hex.ToCharArray();
        var value = HexCharToInt(chars[digitIndex]);

        var maxChange = Math.Max(1, (int)(intensity * 3));
        var change = _random.Next(-maxChange, maxChange + 1);
        value = (value + change + 16) % 16;
        chars[digitIndex] = IntToHexChar(value);

        uint result = 0;
        for (var i = 0; i < 6; i++)
        {
            result = (result << 4) | (uint)HexCharToInt(chars[i]);
        }

        return result;
    }

    public uint MutateSkinToneDigit(uint skinTone, int digitIndex, float intensity)
    {
        var maxChange = Math.Max(1, (int)(intensity * 10));
        var change = _random.Next(-maxChange, maxChange + 1);
        var newValue = (int)skinTone + change;
        return (uint)Math.Clamp(newValue, 0, 100);
    }

    public int MutateHexDigit(int hexCode, int digitIndex, float intensity)
    {
        var hex = hexCode.ToString("X3");
        if (digitIndex < 0 || digitIndex >= 3)
            return hexCode;

        var chars = hex.ToCharArray();
        var value = HexCharToInt(chars[digitIndex]);

        var maxChange = Math.Max(1, (int)(intensity * 3));
        var change = _random.Next(-maxChange, maxChange + 1);
        value = (value + change + 16) % 16;
        chars[digitIndex] = IntToHexChar(value);

        var result = 0;
        for (var i = 0; i < 3; i++)
        {
            result = (result << 4) | HexCharToInt(chars[i]);
        }

        return result;
    }

    private static int HexCharToInt(char c)
    {
        if (c >= '0' && c <= '9')
            return c - '0';

        if (c >= 'A' && c <= 'F')
            return c - 'A' + 10;

        if (c >= 'a' && c <= 'f')
            return c - 'a' + 10;

        return 0;
    }

    private static char IntToHexChar(int value)
        => value < 10 ? (char)('0' + value) : (char)('A' + (value - 10));
}
