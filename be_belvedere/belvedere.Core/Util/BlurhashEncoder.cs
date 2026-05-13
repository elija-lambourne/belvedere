using System;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace belvedere.Core.Util;

public static class BlurhashEncoder
{
    private const string Base83Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$%*+,-.:;=?@[]^_{|}~";

    private static double SRgbToLinear(int value)
    {
        double v = value / 255.0;
        return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    private static int LinearToSrgb(double value)
    {
        double v = Math.Max(0, Math.Min(1, value));
        return (int)Math.Round(v <= 0.0031308 ? v * 12.92 * 255 : (1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055) * 255);
    }

    public static string Encode(Image<Rgba32> image, int compX = 4, int compY = 3)
    {
        int width = image.Width;
        int height = image.Height;

        double[][] factors = new double[compX * compY][];
        for (int y = 0; y < compY; y++)
        for (int x = 0; x < compX; x++)
        {
            double normalisation = (x == 0 && y == 0) ? 1 : 2;
            double[] factor = new double[3];
            for (int j = 0; j < height; j++)
            for (int i = 0; i < width; i++)
            {
                var px = image[i, j];
                double r = SRgbToLinear(px.R);
                double g = SRgbToLinear(px.G);
                double b = SRgbToLinear(px.B);

                double basis = Math.Cos(Math.PI * x * i / width) * Math.Cos(Math.PI * y * j / height);
                factor[0] += basis * r;
                factor[1] += basis * g;
                factor[2] += basis * b;
            }

            factor[0] *= normalisation / (width * height);
            factor[1] *= normalisation / (width * height);
            factor[2] *= normalisation / (width * height);
            factors[y * compX + x] = factor;
        }

        // Encode DC
        var dc = factors[0];
        int dcR = LinearToSrgb(dc[0]);
        int dcG = LinearToSrgb(dc[1]);
        int dcB = LinearToSrgb(dc[2]);
        int dcValue = (dcR << 16) + (dcG << 8) + dcB;

        // Encode AC
        double maxAc = 0;
        for (int i = 1; i < factors.Length; i++)
        {
            maxAc = Math.Max(maxAc, Math.Max(Math.Abs(factors[i][0]), Math.Max(Math.Abs(factors[i][1]), Math.Abs(factors[i][2]))));
        }

        int sizeFlag = (compX - 1) + (compY - 1) * 9;
        int quantMaxAc = Math.Max(0, Math.Min(82, (int)Math.Floor(maxAc * 166 - 0.5)));

        var sb = new System.Text.StringBuilder();

        // Header
        sb.Append(EncodeBase83(sizeFlag, 1));
        sb.Append(EncodeBase83(quantMaxAc, 1));

        // DC
        sb.Append(EncodeBase83(dcValue, 4));

        // AC
        double punch = (quantMaxAc + 1) / 166.0;
        for (int i = 1; i < factors.Length; i++)
        {
            double[] v = factors[i];
            int r = (int)Math.Round(SignPow(v[0] / maxAc, 0.5) * 9);
            int g = (int)Math.Round(SignPow(v[1] / maxAc, 0.5) * 9);
            int b = (int)Math.Round(SignPow(v[2] / maxAc, 0.5) * 9);
            int value = (r + 9 * (g + 9 * b));
            sb.Append(EncodeBase83(value, 2));
        }

        return sb.ToString();
    }

    private static double SignPow(double val, double exp)
    {
        return Math.Sign(val) * Math.Pow(Math.Abs(val), exp);
    }

    private static string EncodeBase83(int value, int length)
    {
        char[] buffer = new char[length];
        for (int i = 1; i <= length; i++)
        {
            int digit = (value / (int)Math.Pow(83, length - i)) % 83;
            buffer[i - 1] = Base83Chars[digit];
        }

        return new string(buffer);
    }
}

