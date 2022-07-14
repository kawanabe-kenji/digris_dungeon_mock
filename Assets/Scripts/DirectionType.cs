using UnityEngine;
using System.Collections.Generic;

namespace DigrisDungeon
{
    public enum DirectionType
    {
        Up = 0,
        Right,
        Down,
        Left,
        Max
    }

    public static class BattleRuleTypeExtensions
    {
        private static readonly Dictionary<DirectionType, Vector2Int> Offsets = new Dictionary<DirectionType, Vector2Int>() {
            {DirectionType.Up, Vector2Int.up},
            {DirectionType.Right, Vector2Int.right},
            {DirectionType.Down, Vector2Int.down},
            {DirectionType.Left, Vector2Int.left},
        };

        public static Vector2Int GetOffset(this DirectionType type)
        {
            return Offsets[type];
        }
    }
}
