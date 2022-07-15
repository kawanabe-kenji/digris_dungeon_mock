using System.Collections.Generic;
using UnityEngine;

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

        private readonly static Dictionary<ShapeType, Vector2Int[]> SHAPE_PATTERN = new Dictionary<ShapeType, Vector2Int[]>()
        {
            { ShapeType.A, new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) } },
            { ShapeType.B, new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) } },
            { ShapeType.C1, new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) } },
            { ShapeType.C2, new Vector2Int[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 0) } },
            { ShapeType.D1, new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) } },
            { ShapeType.D2, new Vector2Int[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0) } },
            { ShapeType.E, new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) } },
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
            Blocks.Add(Vector2Int.zero, new Block());

            var offsets = SHAPE_PATTERN[type];
            Block[] linkedBlocks = new Block[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                Block block = new Block();
                Blocks.Add(offsets[i], block);
                linkedBlocks[i] = block;
            }

            foreach (var kvp in Blocks)
            {
                Block block = kvp.Value;
                foreach (var linkedBlock in linkedBlocks)
                {
                    if (linkedBlock != block)
                        block.LinkedBlocks.Add(linkedBlock);
                }
            }
        }

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
