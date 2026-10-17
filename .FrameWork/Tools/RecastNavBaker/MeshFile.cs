using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Io;

namespace RecastNavBaker
{
    public static class MeshFile
    {
        public const int MaxVertsPerPoly = 6;

        private const int LinkSize = 16;

        private const int MeshHeaderSize = 100;

        public sealed class TileEntry
        {
            public DtMeshData Data;
            public long TileRef;

            public int[] FirstLinks;

            public byte[] RawLinks;
        }

        public sealed class FileData
        {
            public DtNavMeshParams Params;
            public List<TileEntry> Tiles = new();

            public int PolyCount
            {
                get
                {
                    int sum = 0;
                    foreach (TileEntry tile in Tiles)
                    {
                        sum += tile.Data.header.polyCount;
                    }

                    return sum;
                }
            }

            public int VertCount
            {
                get
                {
                    int sum = 0;
                    foreach (TileEntry tile in Tiles)
                    {
                        sum += tile.Data.header.vertCount;
                    }

                    return sum;
                }
            }
        }

        public static FileData Read(string path)
        {
            byte[] raw = File.ReadAllBytes(path);
            RcByteBuffer buf = new(raw);
            buf.Order(RcByteOrder.LITTLE_ENDIAN);

            int magic = buf.GetInt();
            if (magic != NavMeshSetHeader.NAVMESHSET_MAGIC)
            {
                throw new IOException($"无效的 navmesh 文件头：0x{magic:X8}（{path}）");
            }

            int version = buf.GetInt();
            if (version != NavMeshSetHeader.NAVMESHSET_VERSION)
            {
                throw new IOException($"不支持的 navmesh set 版本：{version}（只支持 C 兼容的 version=1）");
            }

            int numTiles = buf.GetInt();
            if (numTiles <= 0)
            {
                throw new IOException("navmesh 文件里没有 tile");
            }

            DtNavMeshParams p = new()
            {
                orig = RcVec3f.Of(buf.GetFloat(), buf.GetFloat(), buf.GetFloat()),
                tileWidth = buf.GetFloat(),
                tileHeight = buf.GetFloat(),
                maxTiles = buf.GetInt(),
                maxPolys = buf.GetInt(),
            };

            DtMeshDataReader meshReader = new();
            FileData fd = new() { Params = p };

            for (int i = 0; i < numTiles; i++)
            {
                TileEntry entry = new()
                {
                    TileRef = buf.GetLong(),
                };
                buf.GetInt();
                buf.GetInt();
                long dataOffset = buf.Position();

                entry.Data = meshReader.Read(buf, MaxVertsPerPoly);
                if (entry.Data?.header == null || entry.Data.header.polyCount <= 0)
                {
                    throw new IOException($"第 {i} 个 tile 解析异常");
                }

                entry.FirstLinks = ReadFirstLinks(raw, dataOffset, entry.Data.header);
                entry.RawLinks = ReadRawLinks(raw, dataOffset, entry.Data.header);
                fd.Tiles.Add(entry);
            }

            if (buf.Position() != raw.Length)
            {
                throw new IOException($"文件尾部还有 {raw.Length - buf.Position()} 字节未解析（numTiles={numTiles}）");
            }

            return fd;
        }

        public static void Write(string path, FileData fd)
        {
            if (fd.Tiles.Count == 0)
            {
                throw new InvalidOperationException("navmesh 里没有 tile");
            }

            List<byte[]> bodies = new(fd.Tiles.Count);
            foreach (TileEntry tile in fd.Tiles)
            {
                using MemoryStream ms = new();
                using (BinaryWriter bw = new(ms, Encoding.UTF8, true))
                {
                    WriteMeshData(bw, tile.Data, tile.FirstLinks, tile.RawLinks);
                }

                bodies.Add(ms.ToArray());
            }

            using FileStream fs = File.Create(path);
            using BinaryWriter outBw = new(fs);

            outBw.Write(NavMeshSetHeader.NAVMESHSET_MAGIC);
            outBw.Write(NavMeshSetHeader.NAVMESHSET_VERSION);
            outBw.Write(fd.Tiles.Count);
            outBw.Write(fd.Params.orig.x);
            outBw.Write(fd.Params.orig.y);
            outBw.Write(fd.Params.orig.z);
            outBw.Write(fd.Params.tileWidth);
            outBw.Write(fd.Params.tileHeight);
            outBw.Write(fd.Params.maxTiles);
            outBw.Write(fd.Params.maxPolys);

            for (int i = 0; i < fd.Tiles.Count; i++)
            {
                outBw.Write(fd.Tiles[i].TileRef);
                outBw.Write(bodies[i].Length);
                outBw.Write(0);
                outBw.Write(bodies[i]);
            }
        }

        private static void WriteMeshData(BinaryWriter bw, DtMeshData d, int[] firstLinks, byte[] rawLinks)
        {
            DtMeshHeader h = d.header;

            bw.Write(h.magic);
            bw.Write(DtMeshHeader.DT_NAVMESH_VERSION);
            bw.Write(h.x);
            bw.Write(h.y);
            bw.Write(h.layer);
            bw.Write(h.userId);
            bw.Write(h.polyCount);
            bw.Write(h.vertCount);
            bw.Write(h.maxLinkCount);
            bw.Write(h.detailMeshCount);
            bw.Write(h.detailVertCount);
            bw.Write(h.detailTriCount);
            bw.Write(h.bvNodeCount);
            bw.Write(h.offMeshConCount);
            bw.Write(h.offMeshBase);
            bw.Write(h.walkableHeight);
            bw.Write(h.walkableRadius);
            bw.Write(h.walkableClimb);
            bw.Write(h.bmin.x);
            bw.Write(h.bmin.y);
            bw.Write(h.bmin.z);
            bw.Write(h.bmax.x);
            bw.Write(h.bmax.y);
            bw.Write(h.bmax.z);
            bw.Write(h.bvQuantFactor);

            for (int i = 0; i < h.vertCount * 3; i++)
            {
                bw.Write(d.verts[i]);
            }

            for (int i = 0; i < h.polyCount; i++)
            {
                DtPoly poly = d.polys[i];
                bw.Write(firstLinks != null ? firstLinks[i] : 0);
                for (int j = 0; j < poly.verts.Length; j++)
                {
                    bw.Write((ushort)poly.verts[j]);
                }

                for (int j = 0; j < poly.neis.Length; j++)
                {
                    bw.Write((ushort)poly.neis[j]);
                }

                bw.Write((ushort)poly.flags);
                bw.Write((byte)poly.vertCount);
                bw.Write((byte)poly.areaAndtype);
            }

            bw.Write(rawLinks ?? new byte[h.maxLinkCount * LinkSize]);

            for (int i = 0; i < h.detailMeshCount; i++)
            {
                DtPolyDetail dm = d.detailMeshes[i];
                bw.Write(dm.vertBase);
                bw.Write(dm.triBase);
                bw.Write((byte)dm.vertCount);
                bw.Write((byte)dm.triCount);
                bw.Write((short)0);
            }

            for (int i = 0; i < h.detailVertCount * 3; i++)
            {
                bw.Write(d.detailVerts[i]);
            }

            for (int i = 0; i < h.detailTriCount * 4; i++)
            {
                bw.Write((byte)d.detailTris[i]);
            }

            for (int i = 0; i < h.bvNodeCount; i++)
            {
                DtBVNode node = d.bvTree[i];
                for (int j = 0; j < 3; j++)
                {
                    bw.Write((ushort)node.bmin[j]);
                }

                for (int j = 0; j < 3; j++)
                {
                    bw.Write((ushort)node.bmax[j]);
                }

                bw.Write(node.i);
            }

            for (int i = 0; i < h.offMeshConCount; i++)
            {
                DtOffMeshConnection con = d.offMeshCons[i];
                for (int j = 0; j < 6; j++)
                {
                    bw.Write(con.pos[j]);
                }

                bw.Write(con.rad);
                bw.Write((ushort)con.poly);
                bw.Write((byte)con.flags);
                bw.Write((byte)con.side);
                bw.Write(con.userId);
            }
        }

        private static int[] ReadFirstLinks(byte[] raw, long tileDataOffset, DtMeshHeader header)
        {
            int polyBase = (int)tileDataOffset + MeshHeaderSize + header.vertCount * 12;
            int stride = PolyStride();
            if (polyBase + header.polyCount * stride > raw.Length)
            {
                return null;
            }

            int[] firstLinks = new int[header.polyCount];
            for (int i = 0; i < firstLinks.Length; i++)
            {
                firstLinks[i] = BitConverter.ToInt32(raw, polyBase + i * stride);
            }

            return firstLinks;
        }

        private static byte[] ReadRawLinks(byte[] raw, long tileDataOffset, DtMeshHeader header)
        {
            int stride = PolyStride();
            int linkBase = (int)tileDataOffset + MeshHeaderSize + header.vertCount * 12 + header.polyCount * stride;
            int length = header.maxLinkCount * LinkSize;
            if (linkBase + length > raw.Length)
            {
                return null;
            }

            byte[] links = new byte[length];
            Array.Copy(raw, linkBase, links, 0, length);
            return links;
        }

        private static int PolyStride()
        {
            return 4 + MaxVertsPerPoly * 2 + MaxVertsPerPoly * 2 + 2 + 1 + 1;
        }
    }
}
