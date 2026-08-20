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

/// <summary>Flying Rat positions in Algonquin.</summary>
internal static class PigeonsAlgonquin
{
    internal static readonly IReadOnlyList<KnownCollectible> Algonquin =
    [
        new(new Vec3(-249.48f, 1771.16f, 1.96f), "Northwood"),
        new(new Vec3(-427.26f, 1551.23f, 21.98f), "Northwood"),
        new(new Vec3(-319.93f, 1509.29f, 19.063f), "Northwood"),
        new(new Vec3(-205.94f, 1509.28f, 26.808f), "Northwood"),
        new(new Vec3(-9.76f, 1497.03f, 18.867f), "Northwood"),
        new(new Vec3(-609.62f, 1397.3f, 8.37f), "North Holland"),
        new(new Vec3(-441.32f, 1288f, 42.015f), "North Holland"),
        new(new Vec3(-414.68f, 1308f, 93.905f), "North Holland"),
        new(new Vec3(-369f, 1279f, 23.255f), "North Holland"),
        new(new Vec3(-207.06f, 1233.33f, 22.04f), "East Holland"),
        new(new Vec3(-30.36f, 1393.96f, 30.125f), "/Northwood"),
        new(new Vec3(141.23f, 1303.44f, 2.652f), "East Holland"),
        new(new Vec3(-680.34f, 1166.44f, 11.08f), "Hickey Bridge"),
        new(new Vec3(-668.63f, 1154.48f, 19.39f), "Hickey Bridge"),
        new(new Vec3(-502.85f, 1125.48f, 11.905f), "Varsity Heights"),
        new(new Vec3(-466.78f, 1018.62f, 11.872f), "Varsity Heights"),
        new(new Vec3(-209.18f, 1041.11f, 10.967f), "Middle Park"),
        new(new Vec3(-595.56f, 846f, 11.825f), "Middle Park West"),
        new(new Vec3(-393.73f, 873.82f, 18.275f), "Middle Park West"),
        new(new Vec3(-72.98f, 942.98f, 20.365f), "Middle Park West"),
        new(new Vec3(-25.51f, 840.46f, 18.612f), "Middle Park"),
        new(new Vec3(116.9f, 917.86f, 15.162f), "Lancaster"),
        new(new Vec3(344.64f, 1010f, 35.15f), "East Borough Bridge"),
        new(new Vec3(451.5f, 1112.56f, 3.565f), "Charge Island"),
        new(new Vec3(-521.56f, 643.94f, 12.77f), "Middle Park West"),
        new(new Vec3(-263.38f, 710.1f, 12.74f), "Middle Park West"),
        new(new Vec3(-35.41f, 721.84f, 18.947f), "Middle Park West"),
        new(new Vec3(279.64f, 683.48f, 4.34f), "Humboldt River"),
        new(new Vec3(581.39f, 727.9f, 2.098f), "Charge Island"),
        new(new Vec3(-420.25f, 435.71f, 12.48f), "Purgatory"),
        new(new Vec3(-115.67f, 429.15f, 17.44f), "Star Junction"),
        new(new Vec3(7.79f, 411.9f, 89.47f), "Hatton Gardens"),
        new(new Vec3(145.1f, 478.12f, 18.85f), "Hatton Gardens"),
        new(new Vec3(-503.98f, 282.26f, 19.731f), "Westminster"),
        new(new Vec3(-287.41f, 236.85f, 204.392f), "Star Junction"),
        new(new Vec3(-247.77f, 243.06f, 16.195f), "Star Junction"),
        new(new Vec3(-180.08f, 210f, 17.49f), "Star Junction"),
        new(new Vec3(41f, 109.88f, 15.015f), "Easton"),
        new(new Vec3(156f, 226.32f, 21.025f), "Lancet"),
        new(new Vec3(202f, 266f, 7.43f), "Lancet"),
        new(new Vec3(428.09f, 238.91f, 14.7f), "Colony Island"),
        new(new Vec3(505f, 220.1f, 30.1f), "Colony Island"),
        new(new Vec3(378.33f, 123.22f, 5.757f), "Colony Island"),
        new(new Vec3(483.71f, 100.54f, 7.738f), "Colony Island"),
        new(new Vec3(270.34f, 31.24f, 4.325f), "President's City"),
        new(new Vec3(510.72f, -51.88f, 15.958f), "Colony Island"),
        new(new Vec3(-463.6f, 7.32f, 11.42f), "The Meat Quarter"),
        new(new Vec3(-407.96f, -84.36f, 14.304f), "The Meat Quarter"),
        new(new Vec3(-298f, -84.76f, 335.23f), "The Triangle"),
        new(new Vec3(-124.08f, 15.92f, 31.895f), "The Triangle"),
        new(new Vec3(-68.19f, -91.27f, 18.398f), "The Triangle"),
        new(new Vec3(240f, -172f, 3.98f), "Fishmarket North"),
        new(new Vec3(-454.6f, -255.5f, 6.91f), "Suffolk"),
        new(new Vec3(-319.983f, -291.555f, 13.75f), "Suffolk"),
        new(new Vec3(-284.57f, -391.71f, 9f), "City Hall"),
        new(new Vec3(-85.66f, -341.33f, 14.87f), "City Hall"),
        new(new Vec3(0241.22f, -417.16f, 8.015f), "Fishmarket South"),
        new(new Vec3(493.85f, -389.98f, 85.427f), "Broker Bridge"),
        new(new Vec3(337.66f, -640.09f, 4.528f), "Fishmarket South"),
        new(new Vec3(28.48f, -599.05f, 14.568f), "The Exchange"),
        new(new Vec3(105.61f, -759.86f, 3.945f), "The Exchange"),
        new(new Vec3(-117.27f, -706.5f, 10.7f), "The Exchange"),
        new(new Vec3(-362.27f, -676.65f, 2.252f), "Castle Garden City"),
        new(new Vec3(114f, -962f, 4.18f), "Castle Gardens"),
        new(new Vec3(-27.92f, -954f, 12.13f), "Castle Gardens"),
        new(new Vec3(-607.94f, -739.68f, 20.756f), "Happiness Island"),
        new(new Vec3(-616.71f, -752.78f, 72.82f), "Happiness Island"),
        new(new Vec3(-611.62f, -753.64f, 78.358f), "Happiness Island"),
        new(new Vec3(-608.92f, -751.21f, 83.75f), "Happiness Island"),
        new(new Vec3(-606.24f, -748.39f, 91.15f), "Happiness Island"),
        new(new Vec3(-606.395f, -748.9f, 92.46f), "Happiness Island"),
    ];
}
