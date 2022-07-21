using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DigrisDungeon
{
    public class Mino
    {
        #region Constants
        public enum ShapeType
        {
            /// <summary> 長い棒 </summary>
            A = 0,
            /// <summary> 正方形 </summary>
            B,
            /// <summary> S字 </summary>
            C1,
            /// <summary> S字(反転) </summary>
            C2,
            /// <summary> L字 </summary>
            D1,
            /// <summary> L字(反転) </summary>
            D2,
            /// <summary> T字 </summary>
            E,
            Max
        }

        public enum UnitType
        {
            Default = 0,
            Route,
            Summon,
            Max
        }

        private readonly static Dictionary<ShapeType, Vector2Int[]> SHAPE_PATTERN = new Dictionary<ShapeType, Vector2Int[]>()
        {
            { ShapeType.A, new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) } },
            { ShapeType.B, new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) } },
            { ShapeType.C1, new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) } },
            { ShapeType.C2, new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 0) } },
            { ShapeType.D1, new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) } },
            { ShapeType.D2, new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0) } },
            { ShapeType.E, new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) } },
        };

        private readonly static Dictionary<ShapeType, int[]> UNIT_ONE_PATTERN = new Dictionary<ShapeType, int[]>()
        {
            { ShapeType.A, new int[] { 3 } },
            { ShapeType.B, new int[] { 3 } },
            { ShapeType.C1, new int[] { 3 } },
            { ShapeType.C2, new int[] { 1 } },
            { ShapeType.D1, new int[] { 1, 3 } },
            { ShapeType.D2, new int[] { 1, 3 } },
            { ShapeType.E, new int[] { 1, 2, 3 } },
        };

        private readonly static Dictionary<ShapeType, int[][]> UNIT_TWO_PATTERN = new Dictionary<ShapeType, int[][]>()
        {
            { ShapeType.A, new int[][] { new int[] { 2, 3 } } },
            { ShapeType.B, new int[][] { new int[] { 1, 3 } } },
            { ShapeType.C1, new int[][] { new int[] { 2, 3 } } },
            { ShapeType.C2, new int[][] { new int[] { 1, 2 } } },
            { ShapeType.D1, new int[][] { new int[] { 0, 1 }, new int[] { 2, 3 } } },
            { ShapeType.D2, new int[][] { new int[] { 0, 3 }, new int[] { 1, 2 } } },
            { ShapeType.E, new int[][] { new int[] { 0, 1 }, new int[] { 0, 2 }, new int[] { 0, 3 } } },
        };
        #endregion // Constant

        #region Variables
        private Dictionary<Vector2Int, Block> _blocks = new Dictionary<Vector2Int, Block>();
        public Dictionary<Vector2Int, Block> Blocks => _blocks;

        public Vector2Int BoardPos;

        private ShapeType _type;
        public ShapeType Type => _type;
        #endregion // Variables

        public static Mino Create(ShapeType type)
        {
            var mino = new Mino();
            mino.Respawn(type);
            return mino;
        }

        public static ShapeType RandomShapeType()
        {
            return (ShapeType)Random.Range(0, (int)ShapeType.Max);
        }

        public void Respawn(ShapeType type)
        {
            _type = type;

            Blocks.Clear();

            UnitType unitType = (UnitType)Random.Range(0, (int)UnitType.Max);
			switch(unitType)
			{
                case UnitType.Route: InitBlocksUnitTwo(type, unitType); break;
                case UnitType.Summon: InitBlocksUnitOne(type, unitType); break;
                default:
                    var offsets = SHAPE_PATTERN[type];
                    foreach(var offset in offsets)
                    {
                        Blocks.Add(offset, new Block());
                    }
                    break;
            }

            foreach (var kvp in Blocks)
            {
                Vector2Int offset = kvp.Key;
                Block block = kvp.Value;
                foreach (var kvp2 in Blocks)
                {
                    Vector2Int linkedOffset = kvp2.Key;
                    // 隣接ブロックをLinkedBlocksに登録
                    if (GameManager.Distance(offset, linkedOffset) == 1)
                    {
                        Block linkedBlock = kvp2.Value;
                        block.LinkedBlocks.Add(linkedBlock);
                    }
                }
            }
        }

        #region Init Unit
        private void InitBlocksUnitOne(ShapeType shapeType, UnitType unitType)
        {
            var offsets = SHAPE_PATTERN[shapeType];

            int[] unitPattern = UNIT_ONE_PATTERN[shapeType];
            int targetIndex = unitPattern[Random.Range(0, unitPattern.Length)];

            for(int i = 0; i < offsets.Length; i++)
            {
                Vector2Int offset = offsets[i];
                Block block = new Block();
                Blocks.Add(offset, block);
                if(i == targetIndex) SetData(block, unitType);
            }
        }

        private void InitBlocksUnitTwo(ShapeType shapeType, UnitType unitType)
        {
            var offsets = SHAPE_PATTERN[shapeType];

            int[][] unitPattern = UNIT_TWO_PATTERN[shapeType];
            int[] targetIndexs = unitPattern[Random.Range(0, unitPattern.Length)];

            for(int i = 0; i < offsets.Length; i++)
            {
                Vector2Int offset = offsets[i];
                Block block = new Block();
                Blocks.Add(offset, block);
                if(Array.IndexOf(targetIndexs, i) >= 0) SetData(block, unitType);
            }
        }

        private void SetData(Block data, UnitType unitType)
        {
            switch(unitType)
            {
                case UnitType.Route:
                    data.IsRounte = true;
                    break;
                case UnitType.Summon:
                    data.Summon = new Summon(1);
                    break;
            }
        }
        #endregion // Init Unit

        public void Rotate()
        {
            var newBlocks = new Dictionary<Vector2Int, Block>();
            foreach (var kvp in Blocks)
            {
                var index = kvp.Key;
                var block = kvp.Value;
                // 回転に合わせてブロックの相対的な位置を変える
                newBlocks.Add(new Vector2Int(index.y, -index.x), block);
            }
            Blocks.Clear();
            _blocks = newBlocks;
        }
    }
}
