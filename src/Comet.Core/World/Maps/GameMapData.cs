// #define NEW_DMAP

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Comet.Shared;
using Microsoft.Extensions.Configuration.Ini;

namespace Comet.Core.World.Maps
{
    public class GameMapData
    {
        private readonly uint mIdDoc;
        private readonly List<MapObject> mMapObjects = new();
        private readonly List<PassageData> mPassageData = new();

        private readonly List<List<Layer>> mSetLayer = new();
        private Tile[,] mCell;
        private Tile mDefault;

        public GameMapData(uint idMapDoc)
        {
            mIdDoc = idMapDoc;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public ref Tile this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return ref mDefault;

                return ref mCell[x, y];
            }
        }

        public int GetPassage(int x, int y)
        {
            for (var cx = 0; cx < 9; cx++)
            for (var cy = 0; cy < 9; cy++)
            {
                int testX = x + WalkXCoords[cx];
                int testY = y + WalkYCoords[cy];

                for (var i = 0; i < mPassageData.Count; i++)
                    if (mPassageData[i].X == testX
                        && mPassageData[i].Y == testY)
                        return mPassageData[i].Index;
            }

            return -1;
        }

        public bool Load(string path)
        {
            if (FileExists(path))
            {
                string realPath = GetRealPath(path);

                if (string.IsNullOrEmpty(realPath))
                {
                    _ = Log.WriteLogAsync(LogLevel.Warning,
                                          $"Map data for file {mIdDoc} '{path}' (realPath:{realPath}) has not been found.");
                    return false;
                }

                Stream stream = File.OpenRead(realPath);

                BinaryReader reader = new(stream, Encoding.ASCII);

                LoadData(reader);
                LoadPassageData(reader);
                LoadLayerData(reader);

                reader.Close();
                reader.Dispose();
                return true;
            }

            _ = Log.WriteLogAsync(LogLevel.Warning, $"Map data for file {mIdDoc} '{path}' has not been found.");
            return false;
        }

        private void LoadData(BinaryReader reader)
        {
            uint version = reader.ReadUInt32();
            uint data = reader.ReadUInt32();
            reader.ReadBytes(260); // jump ??? why
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();

            mCell = new Tile[Width, Height];
            for (var y = 0; y < Height; y++)
            {
                uint checkSum = 0, tmp = 0;
                for (var x = 0; x < Width; x++)
                {
                    short access = reader.ReadInt16();
                    short surface = reader.ReadInt16();
                    short elevation = reader.ReadInt16();

                    checkSum += (uint) ((uint) access * (surface + y + 1) +
                                        (elevation + 2) * (x + 1 + surface));

                    mCell[x, y] = new Tile(elevation, access, surface);
                }

                tmp = reader.ReadUInt32();
                if (checkSum != tmp)
                    _ = Log.WriteLogAsync(LogLevel.Error,
                                          $"Invalid checksum for block of cells (mapdata: {mIdDoc}), y: {y}");
            }
        }

        private void LoadPassageData(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int index = reader.ReadInt32();

                mPassageData.Add(new PassageData(x, y, index));
            }
        }

        private void LoadLayerData(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
            {
                int type = reader.ReadInt32();

                switch (type)
                {
                    case MAP_COVER:
                        reader.ReadChars(416);
                        break;

                    case MAP_TERRAIN:
                        var file = new string(reader.ReadChars(260));
                        int startX = reader.ReadInt32();
                        int startY = reader.ReadInt32();

                        file = file.Substring(0, file.IndexOf('\0'))
                                   .Replace("\\", Path.DirectorySeparatorChar.ToString());
                        if (File.Exists(file))
                        {
                            //var memory = new MemoryStream(File.ReadAllBytes(file));
                            var scenery = new BinaryReader(File.OpenRead(file));

                            var objTerrain = TerrainObject.CreateNew(scenery);
                            objTerrain.SetPos(new Point(startX, startY));
                            AddMapObject(objTerrain);

                            //memory.Close();
                            scenery.Close();
                            //memory.Dispose();
                            scenery.Dispose();
                        }

                        break;

                    case MAP_SOUND:
                        reader.ReadChars(276);
                        break;

                    case MAP_3DEFFECT:
                        reader.ReadChars(72);
                        break;

                    case MAP_3DEFFECTNEW:
                        reader.ReadChars(276);
                        break;
                }
            }
        }

        public int GetFloorAttr(int x, int y)
        {
            Tile cell = this[x, y];
            if (cell.Equals(mDefault))
                return 0;
            return cell.GetFloorAttr(mSetLayer);
        }

        public int GetFloorAlt(int x, int y)
        {
            Tile cell = this[x, y];
            if (cell.Equals(mDefault))
                return 0;
            return cell.GetFloorAlt(mSetLayer);
        }

        public int GetFloorMask(int x, int y)
        {
            Tile cell = this[x, y];
            if (cell.Equals(mDefault))
                return 0;
            return cell.GetFloorMask(mSetLayer);
        }

        /// <summary>
        ///     This method has been created to avoid the case sensitive Linux path system, which would case us some trouble.
        /// </summary>
        private static bool FileExists(string path)
        {
            string name = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(path))
                return false;
            foreach (string file in Directory.GetFiles(name))
                if (file.Equals(
                        path.Replace("\\", Path.DirectorySeparatorChar.ToString())
                            .Replace("/", Path.DirectorySeparatorChar.ToString()),
                        StringComparison.InvariantCultureIgnoreCase))
                    return true;
            return false;
        }

        private static string GetRealPath(string path)
        {
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(path)))
                if (file.Equals(
                        path.Replace("\\", Path.DirectorySeparatorChar.ToString())
                            .Replace("/", Path.DirectorySeparatorChar.ToString()),
                        StringComparison.InvariantCultureIgnoreCase))
                    return file;
            return path;
        }

        public const int MAP_NONE = 0;
        public const int MAP_TERRAIN = 1;
        public const int MAP_TERRAIN_PART = 2;
        public const int MAP_SCENE = 3;
        public const int MAP_COVER = 4;
        public const int MAP_ROLE = 5;
        public const int MAP_HERO = 6;
        public const int MAP_PLAYER = 7;
        public const int MAP_PUZZLE = 8;
        public const int MAP_3DSIMPLE = 9;
        public const int MAP_3DEFFECT = 10;
        public const int MAP_2DITEM = 11;
        public const int MAP_3DNPC = 12;
        public const int MAP_3DOBJ = 13;
        public const int MAP_3DTRACE = 14;
        public const int MAP_SOUND = 15;
        public const int MAP_2DREGION = 16;
        public const int MAP_3DMAGICMAPITEM = 17;
        public const int MAP_3DITEM = 18;
        public const int MAP_3DEFFECTNEW = 19;
        public static readonly sbyte[] WalkXCoords = {0, -1, -1, -1, 0, 1, 1, 1, 0};
        public static readonly sbyte[] WalkYCoords = {1, 1, 0, -1, -1, -1, 0, 1, 0};

        public static readonly sbyte[] RideXCoords =
            {0, -2, -2, -2, 0, 2, 2, 2, 1, 0, -2, 0, 1, 0, 2, 0, 0, -2, 0, -1, 0, 2, 0, 1, 0};

        public static readonly sbyte[] RideYCoords =
            {2, 2, 0, -2, -2, -2, 0, 2, 2, 0, -1, 0, -2, 0, 1, 0, 0, 1, 0, -2, 0, -1, 0, 2, 0};

        #region Indexes

        public static int Pos2Index(int x, int y, int cx, int cy)
        {
            return x + y * cx;
        }

        public static int Index2X(int idx, int cx, int cy)
        {
            return idx % cy;
        }

        public static int Index2Y(int idx, int cx, int cy)
        {
            return idx / cy;
        }

        #endregion

        #region Map Object

        public bool AddMapObject(TerrainObject terrain)
        {
            if (terrain == null)
                return false;

            mMapObjects.Add(terrain);

            return PlaceTerrainObject(terrain);
        }

        public bool DelMapObject(int idx)
        {
            if (idx < 0 || idx >= mMapObjects.Count)
                return false;

            var terrain = mMapObjects[idx] as TerrainObject;
            if (terrain != null && DisplaceTerrainObject(terrain))
            {
                mMapObjects.RemoveAt(idx);
                return true;
            }

            return false;
        }

        public bool PlaceTerrainObject(TerrainObject terrain)
        {
            if (terrain == null)
                return false;

            foreach (TerrainObjectPart part in terrain.Parts)
                for (var j = 0; j < part.SizeBaseCY; j++)
                for (var k = 0; k < part.SizeBaseCX; k++)
                {
                    Layer layer = part.GetLayer(j, k);
                    if (layer.Equals(default))
                        return false;

                    ref Tile tile = ref mCell[part.X - k, part.Y - j];
                    if (tile.Equals(default))
                        return false;

                    tile.AddLayer(mSetLayer, new Layer
                    {
                        Altitude = layer.Altitude,
                        Mask = layer.Mask,
                        Terrain = layer.Terrain
                    });
                }

            return true;
        }

        public bool DisplaceTerrainObject(TerrainObject terrain)
        {
            if (terrain == null)
                return false;

            foreach (TerrainObjectPart part in terrain.Parts)
                for (var j = 0; j < part.SizeBaseCY; j++)
                for (var k = 0; k < part.SizeBaseCX; k++)
                {
                    ref Tile tile = ref mCell[part.X - k, part.Y - j];
                    if (tile.Equals(default))
                        return false;

                    tile.DelLayer(mSetLayer);
                }

            return true;
        }

        #endregion

        #region Terrain Object

        public bool AddTerrainItem(uint idOwner, int x, int y, uint idTerrainType)
        {
            string path = Path.Combine(Environment.CurrentDirectory, "ini");
            IniConfigurationSource source = new();
            string[] content = Directory.GetFiles(path);
            string fileName =
                content.FirstOrDefault(x => x.ToLower()
                                             .EndsWith("terrainnpc.ini", StringComparison.CurrentCultureIgnoreCase));
            path = Path.Combine(path, fileName);
            source.ReloadOnChange = false;
            IniConfigurationProvider reader = new(source);
            try
            {
                reader.Load(File.OpenRead(path));
            }
            catch
            {
                return false;
            }

            string entry = $"NpcType{idTerrainType / 10}:Dir{idTerrainType % 10}";
            if (!reader.TryGet(entry, out path))
                return false;

            var terrain = TerrainObject.CreateNew(new BinaryReader(File.OpenRead(path)), idOwner);
            if (terrain == null)
                return false;

            terrain.SetPos(new Point(x, y));
            if (!AddMapObject(terrain))
                return false;
            return true;
        }

        public bool DelTerrainItem(uint idOwner)
        {
            for (var i = 0; i < mMapObjects.Count; i++)
                if (mMapObjects[i] is TerrainObject terrain)
                    if (terrain.OwnerIdentity == idOwner)
                        return DelMapObject(i);

            return true;
        }

        #endregion
    }

    public struct PassageData
    {
        public PassageData(int x, int y, int index)
        {
            X = x;
            Y = y;
            Index = index;
        }

        public int X;
        public int Y;
        public int Index;
    }
}