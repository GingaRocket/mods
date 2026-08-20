// Coordinate data derived from the pigeon-locator project.
//
// MIT License. Copyright (c) 2018-2026 Wes Hampson.
// https://github.com/whampson/pigeon-locator
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.Generic;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Features.Data;

/// <summary>Flying Rat positions in Alderney.</summary>
internal static class PigeonsAlderney
{
    internal static readonly IReadOnlyList<KnownCollectible> Alderney =
    [
        new(new Vec3(-1169.52f, 1830.78f, 4.08f), "Westdyke"),
        new(new Vec3(-1305.74f, 1696.07f, 32.45f), "Westdyke"),
        new(new Vec3(-956f, 1764.09f, 18.315f), "Westdyke"),
        new(new Vec3(-1366.66f, 1525.61f, 22.983f), "Westdyke"),
        new(new Vec3(-1222.36f, 1457.84f, 22.91f), "Westdyke"),
        new(new Vec3(-1072.14f, 1572.54f, 35.258f), "Westdyke"),
        new(new Vec3(-1498.14f, 1394f, 21.78f), "Leftwood"),
        new(new Vec3(-1314.43f, 1272.01f, 22.195f), "Leftwood"),
        new(new Vec3(-1071.26f, 1282.46f, 38.988f), "Leftwood"),
        new(new Vec3(-929.21f, 1340.54f, 23.865f), "Leftwood"),
        new(new Vec3(-1400.84f, 1148.3f, 18.773f), "Alderney City"),
        new(new Vec3(-1031.76f, 1189.26f, 22.394f), "Leftwood"),
        new(new Vec3(-724f, 1247f, 0.382f), "Leftwood"),
        new(new Vec3(-1242.08f, 1089.48f, 24.885f), "Alderney City"),
        new(new Vec3(-1257.24f, 1076.86f, 22.725f), "On the first-story corner ledge of the office"),
        new(new Vec3(-1392.09f, 995f, 22.755f), "Alderney City"),
        new(new Vec3(-1200.14f, 988.8f, 18.76f), "Alderney City"),
        new(new Vec3(-983.02f, 1031.92f, 30.6f), "Alderney City"),
        new(new Vec3(-1018.54f, 939.05f, 23.79f), "Alderney City"),
        new(new Vec3(-841.02f, 1032f, 15.945f), "Alderney City"),
        new(new Vec3(-1595.77f, 842.06f, 24.28f), "Berchem"),
        new(new Vec3(-1420.84f, 886.82f, 23.638f), "Alderney City"),
        new(new Vec3(-1372.72f, 739.46f, 18.67f), "Alderney City"),
        new(new Vec3(-898.72f, 767f, 6.68f), "Alderney City"),
        new(new Vec3(-846.28f, 826.28f, 3.22f), "Alderney City"),
        new(new Vec3(-1648.84f, 607f, 23.695f), "Berchem"),
        new(new Vec3(-1497.98f, 581f, 22.335f), "Berchem"),
        new(new Vec3(-1246.1f, 626.62f, -1.97f), "Normandy"),
        new(new Vec3(-1184.84f, 639.32f, 8.125f), "Normandy"),
        new(new Vec3(-1599.36f, 509.02f, 31.04f), "Berchem"),
        new(new Vec3(-1324f, 520f, 21.68f), "Acter"),
        new(new Vec3(-1651.24f, 412f, 46.34f), "Acter"),
        new(new Vec3(-1426.21f, 405.17f, 17.886f), "Acter"),
        new(new Vec3(-1366.56f, 282f, 17.54f), "Port Tudor"),
        new(new Vec3(-1262.66f, 230.26f, 4.155f), "Port Tudor"),
        new(new Vec3(-913.48f, 348.3f, 4.794f), "Port Tudor"),
        new(new Vec3(-1995.41f, 199.93f, 15.84f), "Tudor"),
        new(new Vec3(-1768.12f, 262.81f, 21.91f), "Acter"),
        new(new Vec3(-1793f, 113f, 17.9f), "Acter"),
        new(new Vec3(-1598.86f, 148.34f, 13.9f), "Tudor"),
        new(new Vec3(-2248f, -21.34f, 3.37f), "Tudor"),
        new(new Vec3(-2131f, 16f, 15.07f), "Tudor"),
        new(new Vec3(-1990f, -121.8f, 30.477f), "From the Plumbers Skyway on-ramp, climb"),
        new(new Vec3(-1587.87f, 26.4f, 12.77f), "tudor"),
        new(new Vec3(-1435.54f, -71.88f, 33.42f), "Tudor"),
        new(new Vec3(-1034f, 50f, 9.46f), "Port Tudor"),
        new(new Vec3(-1505.73f, -152.5f, 13.19f), "Port Tudor"),
        new(new Vec3(-1703.18f, -328.5f, 1.958f), "Acter Industrial Park"),
        new(new Vec3(-1259.91f, -253.09f, 4.273f), "Acter Industrial Park"),
        new(new Vec3(-1261.51f, -252.25f, 2.065f), "Acter Industrial Park"),
        new(new Vec3(-1034.1f, -298f, 11.783f), "Acter Industrial Park"),
        new(new Vec3(-856.04f, -396.53f, 8.485f), "Acter Industrial Park"),
        new(new Vec3(-2019.48f, -405.73f, 4.063f), "Acter Industrial Park"),
        new(new Vec3(-2017.24f, -493.38f, 6.273f), "Acter Industrial Park"),
        new(new Vec3(-1678.84f, -486.32f, 51.728f), "Acter Industrial Park"),
        new(new Vec3(-1445f, -540.52f, 8.6f), "Acter Industrial Park"),
    ];
}
