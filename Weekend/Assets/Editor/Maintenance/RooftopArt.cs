// 职责：生成可复现的原创像素天台底图；装饰画面与实际场景碰撞分开维护。
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RooftopArt
{
    const int W = 480, H = 256;
    static Texture2D image;
    static Color32 C(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
    static void Rect(int x, int y, int w, int h, string color)
    {
        var c = C(color);
        for (int j = Mathf.Max(0, y); j < Mathf.Min(H, y + h); j++)
            for (int i = Mathf.Max(0, x); i < Mathf.Min(W, x + w); i++)
                image.SetPixel(i, j, c);
    }
    static void Line(int x, int y, int x2, int y2, string color)
    {
        int dx = Mathf.Abs(x2 - x), dy = -Mathf.Abs(y2 - y), sx = x < x2 ? 1 : -1, sy = y < y2 ? 1 : -1, e = dx + dy;
        while (true)
        {
            Rect(x, y, 1, 1, color);
            if (x == x2 && y == y2)
                break;
            int e2 = e * 2;
            if (e2 >= dy)
            {
                e += dy;
                x += sx;
            }
            if (e2 <= dx)
            {
                e += dx;
                y += sy;
            }
        }
    }
    static void Ellipse(int x, int y, int rx, int ry, string color)
    {
        for (int j = -ry; j <= ry; j++)
            for (int i = -rx; i <= rx; i++)
                if ((float)i * i / (rx * rx) + (float)j * j / (ry * ry) <= 1)
                    Rect(x + i, y + j, 1, 1, color);
    }
    public static Sprite Build()
    {
        image = new Texture2D(W, H, TextureFormat.RGBA32, false);
        image.SetPixels32(new Color32[W * H]);
        var random = new System.Random(4015);
        // A bounded silhouette leaves the camera's actual black clear color visible around the roof.
        Rect(10, 10, 460, 236, "22262a");
        Rect(16, 18, 448, 220, "50504a");
        string[] tiles = { "777568", "737163", "7c796b", "716e62", "777164" };
        for (int y = 22; y < 232; y += 16)
            for (int x = 20; x < 460; x += 16)
            {
                Rect(x, y, 15, 15, tiles[random.Next(tiles.Length)]);
                Line(x, y, x + 14, y, "55574e");
                Line(x, y + 14, x + 14, y + 14, "8c8775");
                for (int k = 0; k < 9; k++)
                    Rect(x + random.Next(1, 14), y + random.Next(1, 14), 1, 1, k % 2 == 0 ? "666659" : "87816d");
            }
        // Hairline fractures, repaired tar, weeds and drain channels add age without visual noise.
        Line(151, 153, 159, 149, "4d5049");
        Line(159, 149, 157, 142, "4d5049");
        Line(157, 142, 166, 139, "4d5049");
        Line(327, 63, 333, 71, "4d5049");
        Line(333, 71, 341, 73, "4d5049");
        Rect(23, 26, 433, 3, "484d48");
        Rect(25, 28, 429, 1, "626658");
        foreach (int x in new[] { 27, 440 })
        {
            Rect(x, 29, 13, 11, "353d3b");
            for (int i = 2; i < 12; i += 3)
                Rect(x + i, 30, 1, 9, "778073");
        }
        for (int i = 0; i < 32; i++)
        {
            int x = 25 + random.Next(425);
            if (i % 4 == 0)
            {
                Line(x, 30, x - 2, 34, "4c5944");
                Line(x, 30, x + 2, 33, "677255");
            }
        }
        // Raised masonry parapets: cap, front face, joints, rain marks and chipped edges.
        Rect(10, 232, 460, 14, "3b403e");
        Rect(10, 242, 460, 5, "aaa18a");
        Rect(10, 239, 460, 3, "7c7a69");
        Rect(10, 10, 460, 12, "363b39");
        Rect(10, 20, 460, 5, "99917b");
        Rect(10, 18, 460, 2, "6b6e5f");
        Rect(10, 23, 9, 210, "4a5049");
        Rect(10, 23, 3, 215, "9a947f");
        Rect(461, 23, 9, 215, "343d38");
        Rect(461, 23, 3, 215, "8b8976");
        for (int x = 12; x < 470; x += 32)
        {
            Rect(x, 239, 1, 8, "575e54");
            Rect(x + 7, 233, 2, 5, "2e3934");
            Rect(x, 11, 1, 12, "555b4e");
        }
        // Long rusted pipe with elbow clamps.
        Rect(23, 146, 5, 80, "2f3936");
        Rect(24, 146, 3, 80, "86674d");
        Rect(24, 146, 1, 80, "b09668");
        foreach (int y in new[] { 151, 184, 218 })
        {
            Rect(21, y, 9, 3, "444e43");
            Rect(22, y + 2, 7, 1, "b0a47d");
        }
        // Stairwell roof and cream brick facade, recessed green door, utility meter, tiled treads.
        Rect(48, 63, 90, 63, "3f443f");
        Rect(53, 76, 72, 45, "a0977d");
        Rect(53, 76, 72, 4, "6d6a59");
        for (int y = 81; y < 120; y += 8)
        {
            Line(54, y, 123, y, "797563");
            for (int x = 57 + (y % 16 == 1 ? 0 : 8); x < 124; x += 16)
                Rect(x, y, 1, 8, "89826c");
        }
        Rect(45, 117, 87, 29, "303c3b");
        Rect(47, 120, 83, 24, "59675c");
        for (int y = 122; y < 144; y += 5)
        {
            Rect(47, y, 83, 1, "77816a");
            Rect(47, y - 1, 83, 1, "414e47");
        }
        Rect(44, 114, 90, 5, "262e2e");
        Rect(46, 118, 86, 2, "87917a");
        Rect(73, 75, 28, 36, "323a35");
        Rect(76, 76, 22, 32, "4a6255");
        Rect(78, 94, 18, 11, "283d36");
        Rect(79, 95, 15, 8, "64766a");
        Rect(78, 78, 18, 13, "3e5548");
        Rect(94, 89, 2, 3, "c9b27d");
        Rect(108, 89, 10, 15, "535a50");
        Rect(110, 97, 6, 5, "a4ad8e");
        Rect(111, 98, 4, 3, "53695a");
        Line(113, 89, 113, 80, "3a4640");
        Line(113, 80, 123, 80, "3a4640");
        for (int i = 0; i < 5; i++)
        {
            Rect(70, 56 + i * 3, 35, 3, "787a68");
            Rect(70, 56 + i * 3, 35, 1, "b3aa8c");
        }
        Rect(79, 108, 16, 4, "c2b991");
        Rect(82, 109, 1, 2, "5b6554");
        Rect(86, 109, 1, 2, "5b6554");
        Rect(90, 109, 1, 2, "5b6554");
        // Galvanized water tank: dark contact shadow, steel hoops, lid and patched rust.
        Ellipse(116, 162, 42, 10, "4d5047");
        Rect(88, 165, 4, 14, "343f39");
        Rect(139, 165, 4, 14, "343f39");
        Rect(79, 178, 75, 35, "666e64");
        Ellipse(116, 179, 37, 12, "5f685e");
        Ellipse(116, 213, 37, 13, "b0b3a0");
        Ellipse(116, 213, 33, 9, "7d8d7c");
        Rect(81, 187, 70, 3, "a3a995");
        Rect(81, 191, 70, 1, "485a50");
        Rect(81, 204, 70, 3, "adb19a");
        Rect(81, 208, 70, 1, "4c5d51");
        for (int x = 86; x < 151; x += 9)
        {
            Rect(x, 180, 1, 29, "7c8877");
            Rect(x, 188, 2, 1, "414d44");
        }
        Rect(98, 182, 5, 12, "8b654a");
        Rect(101, 178, 2, 11, "6e5544");
        Rect(144, 192, 3, 11, "876d4f");
        Ellipse(116, 217, 9, 3, "4e6258");
        Rect(113, 219, 6, 3, "a6af98");
        Rect(153, 179, 12, 4, "4a5649");
        Rect(162, 169, 4, 14, "7d775d");
        // Two proper air-conditioning units, fan rings, slatted sides and warning plates.
        foreach (int x in new[] { 194, 252 })
        {
            Rect(x - 3, 49, 47, 34, "454b43");
            Rect(x, 54, 40, 29, "9b9f88");
            Rect(x, 54, 40, 3, "66735f");
            Rect(x, 81, 40, 4, "c0bca1");
            Rect(x + 38, 55, 4, 27, "59675a");
            Ellipse(x + 17, 69, 12, 11, "424f45");
            Ellipse(x + 17, 69, 10, 9, "778574");
            for (int n = 0; n < 4; n++)
            {
                float a = n * Mathf.PI * .5f;
                Line(x + 17, 69, x + 17 + (int)(9 * Mathf.Cos(a)), 69 + (int)(8 * Mathf.Sin(a)), "374b40");
            }
            Ellipse(x + 17, 69, 3, 3, "bac0a0");
            for (int y = 58; y < 80; y += 3)
                Rect(x + 32, y, 5, 1, "536951");
            Rect(x + 3, 56, 7, 3, "b69d69");
            Rect(x + 5, 57, 3, 1, "66583d");
        }
        // Cloth observation pad keeps the existing telescope coordinates readable.
        Rect(323, 146, 52, 39, "51574c");
        Rect(325, 149, 48, 35, "555f55");
        Rect(327, 151, 44, 31, "6c7665");
        for (int y = 153; y < 182; y += 4)
            Rect(328, y, 42, 1, "5a6859");
        for (int x = 328; x < 371; x += 5)
        {
            Rect(x, 148, 2, 2, "a19b7c");
            Rect(x, 184, 2, 2, "a19b7c");
        }
        // Sagging washing line, thin support poles, seams and frayed hems.
        Rect(260, 201, 3, 28, "414c42");
        Rect(395, 201, 3, 28, "414c42");
        Line(261, 226, 325, 221, "c0b79a");
        Line(325, 221, 396, 226, "c0b79a");
        string[] cloth = { "a29178", "6f8172", "9b8172" };
        for (int i = 0; i < 3; i++)
        {
            int x = 280 + i * 32, y = 199 - i % 2 * 3;
            Rect(x + 2, y - 2, 21, 25, "414a40");
            Rect(x, y, 20, 25, cloth[i]);
            Rect(x + 2, y + 2, 1, 20, "b0a18a");
            Rect(x + 17, y + 1, 1, 22, "5a6557");
            Rect(x, y + 2, 20, 1, "b2a388");
            Rect(x + 3, y + 24, 2, 4, "807555");
            Rect(x + 15, y + 24, 2, 4, "807555");
        }
        // A discarded chair, empty pots and an ash-filled tin suggest daily use.
        Rect(405, 69, 23, 4, "504c3d");
        Rect(406, 56, 3, 21, "6b5840");
        Rect(424, 56, 3, 21, "6b5840");
        Rect(406, 75, 22, 14, "80694a");
        Rect(407, 87, 21, 3, "a38c64");
        for (int x = 410; x < 426; x += 5)
            Rect(x, 76, 2, 11, "4e4f3e");
        Rect(405, 70, 23, 5, "a58a61");
        Rect(406, 72, 21, 1, "c2a274");
        foreach (int x in new[] { 418, 441 })
        {
            Ellipse(x, 111, 9, 3, "4b4f42");
            Rect(x - 6, 111, 13, 12, "986b50");
            Ellipse(x, 123, 8, 4, "b18a64");
            Ellipse(x, 124, 5, 2, "454a38");
            Line(x, 125, x - 3, 136, "666f48");
            Line(x - 3, 136, x + 2, 133, "7b8051");
        }
        Ellipse(391, 91, 6, 3, "b4ae8d");
        Ellipse(391, 92, 4, 2, "62664f");
        Rect(168, 47, 7, 4, "a69b7b");
        Line(168, 49, 175, 47, "6b6c57");
        Rect(181, 45, 3, 2, "705a44");
        image.Apply();
        const string path = "Assets/Art/Apartment/Rooftop/Rooftop.png";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
