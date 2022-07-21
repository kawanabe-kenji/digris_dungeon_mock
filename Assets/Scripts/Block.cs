using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigrisDungeon
{
    public class Block
    {
        /// <summary> 地層か否か </summary>
        public bool IsStrata;

        public bool IsRounte;

        public List<Block> LinkedBlocks;

        public Summon Summon;

        public bool IsMino()
        {
            return LinkedBlocks.Count > 0;
        }

        public Block()
        {
            LinkedBlocks = new List<Block>();
        }
    }
}
