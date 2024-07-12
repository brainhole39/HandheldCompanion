﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static HandheldCompanion.Managers.MultimediaManager;

namespace HandheldCompanion.Managers.Desktop;

public class ScreenResolution : IComparable<ScreenResolution>
{
    public SortedList<int, int> Frequencies;
    public int Height;
    public int Width;
    public int BitsPerPel;

    public ScreenResolution(int dmPelsWidth, int dmPelsHeight, int bitsPerPel)
    {
        Width = dmPelsWidth;
        Height = dmPelsHeight;
        BitsPerPel = bitsPerPel;
        Frequencies = new(Comparer<int>.Create((x, y) => y.CompareTo(x)));
    }

    public override string ToString()
    {
        return $"{Width} x {Height}";
    }

    public int CompareTo(ScreenResolution other)
    {
        if (other == null) return 1;

        // First compare widths
        int widthComparison = Width.CompareTo(other.Width);
        if (widthComparison != 0)
        {
            return -widthComparison;
        }

        // If widths are equal, compare heights
        return -Height.CompareTo(other.Height);
    }

    public override bool Equals(object obj)
    {
        if (obj is ScreenResolution other)
        {
            return Width == other.Width && Height == other.Height;
        }
        return false;
    }

    public override int GetHashCode()
    {
        // Combine Width and Height into a single hash code
        return HashCode.Combine(Width, Height);
    }
}

public class ScreenFramelimit
{
    public int index;
    public int limit;

    public ScreenFramelimit(int index, int limit)
    {
        this.index = index;
        this.limit = limit;
    }

    public override string ToString()
    {
        if (limit == 0)
            return "Disabled";

        return $"{limit} FPS";
    }
}

public class ScreenDivider
{
    public int divider;
    public ScreenResolution resolution;

    public ScreenDivider(int divider, ScreenResolution resolution)
    {
        this.divider = divider;
        this.resolution = resolution;
    }

    // todo: localize me
    public override string ToString()
    {
        return $"1/{divider} ({resolution.Width}x{resolution.Height})";
    }
}

public struct ScreenRotation
{
    public enum Rotations
    {
        UNSET = -1,
        DEFAULT = 0,
        D90 = 1,
        D180 = 2,
        D270 = 3
    }

    public Rotations rotation;
    public Rotations rotationNativeBase;
    public Rotations rotationUnnormalized;

    public ScreenRotation()
    {
        rotationUnnormalized = Rotations.DEFAULT;
        rotationNativeBase = Rotations.DEFAULT;
        rotation = Rotations.DEFAULT;
    }

    public ScreenRotation(Rotations unnormalized, Rotations native)
    {
        rotationUnnormalized = unnormalized;

        if (native == Rotations.UNSET)
            rotationNativeBase = (Rotations)((4 - (int)unnormalized) % 4);
        else
            rotationNativeBase = native;

        rotation = (Rotations)(((int)unnormalized + (int)rotationNativeBase) % 4);
    }

    public static implicit operator Rotations(ScreenRotation r)
    {
        return r.rotation;
    }

    public static implicit operator ScreenOrientation(ScreenRotation r)
    {
        return (ScreenOrientation)r.rotation;
    }

    public override string ToString()
    {
        switch (rotation)
        {
            case Rotations.DEFAULT:
            case Rotations.D90:
            case Rotations.D180:
            case Rotations.D270:
                return $"{((int)rotation * 90).ToString()}°";
            default:
                return "undefined";
        }
    }
}

public class DesktopScreen
{
    public DisplayDevice devMode;
    public Screen screen;
    public string DevicePath;
    public string FriendlyName;
    public bool IsPrimary => screen.Primary;

    public List<ScreenResolution> screenResolutions = [];
    public List<ScreenDivider> screenDividers = [];

    private static Dictionary<int, List<ScreenFramelimit>> _cachedFrameLimits = [];

    public DesktopScreen(Screen screen)
    {
        this.screen = screen;

        devMode = GetDisplay(screen.DeviceName);
        FriendlyName = GetDisplayFriendlyName(screen.DeviceName);
        DevicePath = GetDisplayPath(screen.DeviceName);
    }

    public override string ToString()
    {
        return FriendlyName; 
    }

    public bool HasResolution(ScreenResolution resolution)
    {
        return screenResolutions.Count(a => a.Width == resolution.Width && a.Height == resolution.Height) > 0;
    }

    public ScreenResolution GetResolution()
    {
        // todo: improve me
        // that's a dirty way to manage native portrait display or rotated display
        ScreenResolution resolution = screenResolutions.FirstOrDefault(a => a.Width == devMode.dmPelsWidth && a.Height == devMode.dmPelsHeight);
        if (resolution is null)
            resolution = screenResolutions.FirstOrDefault(a => a.Width == devMode.dmPelsHeight && a.Height == devMode.dmPelsWidth);

        return resolution;
    }

    public int GetCurrentFrequency()
    {
        return devMode.dmDisplayFrequency;
    }

    // A function that takes a screen frequency int value and returns a list of integer values that are the quotient of the frequency and the closest divisor
    public List<ScreenFramelimit> GetFramelimits()
    {
        // A list to store the quotients
        List<ScreenFramelimit> Limits = [new(0, 0)]; // (Comparer<int>.Create((x, y) => y.CompareTo(x)));

        // A variable to store the divider value, rounded to nearest even number
        int divider = 1;
        int dmDisplayFrequency = RoundToEven(devMode.dmDisplayFrequency);

        if (_cachedFrameLimits.ContainsKey(dmDisplayFrequency)) { return _cachedFrameLimits[dmDisplayFrequency]; }

        int lowestFPS = dmDisplayFrequency;

        HashSet<int> fpsLimits = [];

        // A loop to find the lowest possible fps limit option and limits from division
        do
        {
            // If the frequency is divisible by the divider, add the quotient to the list
            if (dmDisplayFrequency % divider == 0)
            {
                int frequency = dmDisplayFrequency / divider;
                if (frequency < 20)
                {
                    break;
                }
                fpsLimits.Add(frequency);
                lowestFPS = frequency;
            }

            // Increase the divider by 1
            divider++;
        } while (true);

        // loop to fill all possible fps limit options from lowest fps limit (e.g. getting 40FPS dor 60Hz)
        int nrOptions = dmDisplayFrequency / lowestFPS;
        for (int i = 1; i < nrOptions; i++)
        {
            fpsLimits.Add(lowestFPS * i);
        }

        // Fill limits

        var orderedFpsLimits = fpsLimits.OrderByDescending(f => f);

        for (int i = 0; i < orderedFpsLimits.Count(); i++)
        {
            Limits.Add(new(i + 1, orderedFpsLimits.ElementAt(i)));
        }

        _cachedFrameLimits.Add(dmDisplayFrequency, Limits);
        // Return the list of quotients
        return Limits;
    }

    public ScreenFramelimit GetClosest(int fps)
    {
        List<ScreenFramelimit> limits = GetFramelimits();

        ScreenFramelimit? fpsInLimits = limits.FirstOrDefault(l => l.limit == fps);
        if (fpsInLimits is not null) { return fpsInLimits; }

        var diffs = GetFramelimits().Select(limit => (Math.Abs(fps - limit.limit), limit))
                                    .OrderBy(g => g.Item1).ThenBy(g => g.limit.limit).ToList();

        var lowestDiff = diffs.First().Item1;
        var lowestDiffs = diffs.Where(d => d.Item1 == lowestDiff);

        return lowestDiffs.Last().limit;
    }

    // A function that takes an int as a parameter and returns the closest multiple of 10
    private int RoundToEven(int num)
    {
        if (num % 2 == 0)
            return num;

        return num + 1;
    }
}