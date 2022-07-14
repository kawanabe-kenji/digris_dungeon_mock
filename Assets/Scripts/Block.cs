using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigrisDungeon
{
    public class Block
    {
        public Block[] LinkedBlocks;

        public Block()
        {
            LinkedBlocks = new Block[(int)DirectionType.Max];
        }
    }
}
