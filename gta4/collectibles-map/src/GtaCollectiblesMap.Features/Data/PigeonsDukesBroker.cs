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

/// <summary>Flying Rat positions in Dukes and Broker.</summary>
internal static class PigeonsDukesBroker
{
    internal static readonly IReadOnlyList<KnownCollectible> DukesBroker =
    [
        new(new Vec3(1414f, 1242f, 1.45f), "Dukes Bay Bridge"),
        new(new Vec3(909.86f, 941.18f, 11.86f), "Steinway"),
        new(new Vec3(1095.84f, 999.46f, 15.355f), "Steinway"),
        new(new Vec3(1252.98f, 994.53f, 15.047f), "Steinway"),
        new(new Vec3(1254.6f, 996.55f, 12.74f), "Steinway"),
        new(new Vec3(1118.95f, 914.51f, 32.21f), "Steinway"),
        new(new Vec3(1271.25f, 895.93f, 30.775f), "Steinway"),
        new(new Vec3(744.34f, 606.49f, 34.86f), "East Borough Bridge"),
        new(new Vec3(860f, 587f, 9.195f), "East Island City"),
        new(new Vec3(893.5f, 702f, 18.972f), "Steinway"),
        new(new Vec3(1169.19f, 777.56f, 37.79f), "Steinway"),
        new(new Vec3(1148.74f, 686.82f, 39.8f), "Steinway"),
        new(new Vec3(1270.12f, 772.34f, 51.673f), "East Island City"),
        new(new Vec3(1320f, 666f, 50.55f), "East Island City"),
        new(new Vec3(1609f, 852f, 14.85f), "Meadows Park"),
        new(new Vec3(1489.13f, 614.275f, 29.553f), "Meadows Park"),
        new(new Vec3(1886.12f, 781.04f, 22.33f), "Francis International Airport"),
        new(new Vec3(1748.98f, 651.22f, 33.475f), "Willis"),
        new(new Vec3(1619.84f, 443.09f, 43.247f), "Meadow Hills"),
        new(new Vec3(1823.34f, 397.18f, 32.03f), "Willis"),
        new(new Vec3(2303.47f, 616.15f, 16.02f), "Francis International Airport"),
        new(new Vec3(2367.38f, 368f, 10.68f), "Francis International Airport"),
        new(new Vec3(2317.83f, 336.87f, 6.49f), "Francis International Airport"),
        new(new Vec3(2618.475f, 416f, 79.835f), "Francis International Airport"),
        new(new Vec3(948.05f, 416.73f, 17.085f), "BOABO"),
        new(new Vec3(1175.78f, 439.37f, 32.385f), "Cerveza Heights"),
        new(new Vec3(1382.54f, 532f, 44.908f), "Meadows Park"),
        new(new Vec3(1411f, 405.42f, 35.12f), "Cerveza Heights"),
        new(new Vec3(652f, 242.72f, 42.615f), "Algonquin Bridge"),
        new(new Vec3(795.66f, 130.3f, 11.112f), "BOABO"),
        new(new Vec3(1140.54f, 234.4f, 35.276f), "Schottler"),
        new(new Vec3(1303.61f, 162.83f, 33.09f), "Schottler"),
        new(new Vec3(1433.78f, 206.45f, 31.575f), "Beechwood City"),
        new(new Vec3(1602.08f, 175f, 22.48f), "Beechwood City"),
        new(new Vec3(1084.14f, 38.68f, 37.493f), "Downtown"),
        new(new Vec3(1328.84f, -43.45f, 27.308f), "South Slopes"),
        new(new Vec3(791.1f, -233f, 21.07f), "East Hook"),
        new(new Vec3(853.46f, -176.96f, 13.86f), "East Hook"),
        new(new Vec3(1069.46f, -170.66f, 30.052f), "Outlook"),
        new(new Vec3(1308f, -175f, 27.52f), "South Slopes"),
        new(new Vec3(956.22f, -292.42f, 24.628f), "Hove Beach"),
        new(new Vec3(1288.84f, -316.48f, 24.05f), "Firefly Projects"),
        new(new Vec3(724.28f, -440.55f, 2.265f), "Hove Beach"),
        new(new Vec3(1162.44f, -458f, 17.195f), "Hove Beach"),
        new(new Vec3(1513.12f, -420.3f, 32.502f), "Firefly Projects"),
        new(new Vec3(822.59f, -585.99f, 16.785f), "Firefly Island"),
        new(new Vec3(1006.105f, -655f, 17.613f), "Firefly Island"),
        new(new Vec3(1153.08f, -589.14f, 39.51f), "Hove Beach"),
        new(new Vec3(1312.18f, -508.95f, 14.94f), "Firefly Projects"),
        new(new Vec3(932f, -849.12f, 0.541f), "Firefly Island"),
        new(new Vec3(1384.9f, -739.52f, 9.417f), "Beachgate"),
    ];
}
