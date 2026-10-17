using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DotRecast.Recast.Geom;

namespace RecastNavBaker
{
    public static class ObjGeometry
    {
        public static SimpleInputGeomProvider Load(string path)
        {
            List<float> vertices = new();
            List<int> faces = new();

            int lineNumber = 0;
            foreach (string raw in File.ReadLines(path))
            {
                lineNumber++;
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0])
                {
                    case "v":
                        if (parts.Length < 4)
                        {
                            throw new FormatException($"第 {lineNumber} 行顶点格式错误：{line}");
                        }

                        vertices.Add(Float(parts[1]));
                        vertices.Add(Float(parts[2]));
                        vertices.Add(Float(parts[3]));
                        break;
                    case "f":
                        ReadFace(parts, vertices.Count / 3, faces, line, lineNumber);
                        break;
                }
            }

            if (vertices.Count == 0 || faces.Count == 0)
            {
                throw new InvalidOperationException($"OBJ 里没有可用几何：{path}");
            }

            return new SimpleInputGeomProvider(vertices, faces);
        }

        private static void ReadFace(string[] parts, int vertexCount, List<int> faces, string line, int lineNumber)
        {
            int count = parts.Length - 1;
            if (count < 3)
            {
                throw new FormatException($"第 {lineNumber} 行面片顶点数不足：{line}");
            }

            int first = Index(parts[1], vertexCount);
            for (int i = 0; i < count - 2; i++)
            {
                faces.Add(first);
                faces.Add(Index(parts[2 + i], vertexCount));
                faces.Add(Index(parts[3 + i], vertexCount));
            }
        }

        private static int Index(string token, int vertexCount)
        {
            string text = token.Split('/')[0];
            int index = int.Parse(text, CultureInfo.InvariantCulture);
            int resolved = index > 0 ? index - 1 : vertexCount + index;
            if (resolved < 0 || resolved >= vertexCount)
            {
                throw new FormatException($"面片引用了不存在的顶点：{token}（当前顶点数 {vertexCount}）");
            }

            return resolved;
        }

        private static float Float(string text)
        {
            return float.Parse(text, CultureInfo.InvariantCulture);
        }
    }
}
