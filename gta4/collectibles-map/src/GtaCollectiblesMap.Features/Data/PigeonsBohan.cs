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

/// <summary>Flying Rat positions in Bohan.</summary>
internal static class PigeonsBohan
{
    internal static readonly IReadOnlyList<KnownCollectible> Bohan =
    [
        new(new Vec3(410.42f, 2053.21f, 7.252f), "Boulevard"),
        new(new Vec3(746.96f, 2096.22f, 0.326f), "Boulevard"),
        new(new Vec3(610.22f, 1868.56f, 32.11f), "Boulevard"),
        new(new Vec3(909.99f, 1894.83f, 37.115f), "Northern Gardens"),
        new(new Vec3(626.59f, 1756.49f, 39.383f), "Boulevard"),
        new(new Vec3(807.54f, 1805.71f, 38.67f), "Boulevard"),
        new(new Vec3(1074.9f, 1821.2f, 13.32f), "Northern Gardens"),
        new(new Vec3(1256.98f, 1834.21f, 10.135f), "Northern Gardens"),
        new(new Vec3(1507.46f, 1822.24f, 1.9f), "Little Bay"),
        new(new Vec3(397.7f, 1709.24f, 18.432f), "Fortside"),
        new(new Vec3(390.23f, 1656.38f, 15.4f), "Fortside"),
        new(new Vec3(482.58f, 1496.73f, 16.806f), "South Bohan"),
        new(new Vec3(482.58f, 1495.37f, 13.07f), "South Bohan"),
        new(new Vec3(572.84f, 1505.42f, 22.158f), "South Bohan"),
        new(new Vec3(572.98f, 1507f, 22.158f), "South Bohan"),
        new(new Vec3(968.92f, 1629f, 32.45f), "Industrial"),
        new(new Vec3(1031.62f, 1573f, 9.125f), "Industrial"),
        new(new Vec3(1251.81f, 1557.32f, 20.8f), "Industrial"),
        new(new Vec3(299.44f, 1361.46f, 8.38f), "South Bohan"),
        new(new Vec3(512.38f, 1246.44f, 1.655f), "South Bohan"),
        new(new Vec3(701.3f, 1289.44f, 10.255f), "South Bohan/Chase Point"),
        new(new Vec3(785.17f, 1402.99f, 15.52f), "Chase Point"),
    ];
}
