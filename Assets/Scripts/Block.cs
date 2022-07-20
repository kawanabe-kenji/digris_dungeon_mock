using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigrisDungeon
{
    public class Block
    {
        /// <summary> 地層か否か </summary>
        public bool IsStrata;

        public Block[] LinkedBlocks;

        public Block()
        {
            LinkedBlocks = new Block[(int)DirectionType.Max];
        }
    }
}
