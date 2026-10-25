using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnakeEscape.Domain
{
    /// <summary>
    /// Immutable grid coordinate. The authoritative position type for all puzzle logic.
    /// Column increases rightward; Row increases upward.
    /// </summary>
    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        [SerializeField] private int col;
        [SerializeField] private int row;

        public int Col => col;
        public int Row => row;

        public GridCoord(int col, int row)
        {
            this.col = col;
            this.row = row;
        }

        // --- Cardinal constants ---
        public static readonly GridCoord Zero  = new GridCoord(0,  0);
        public static readonly GridCoord Up    = new GridCoord(0,  1);
        public static readonly GridCoord Down  = new GridCoord(0, -1);
        public static readonly GridCoord Left  = new GridCoord(-1, 0);
        public static readonly GridCoord Right = new GridCoord(1,  0);

        public static readonly GridCoord[] CardinalDirections = { Up, Down, Left, Right };

        // --- Operators ---
        public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.Col + b.Col, a.Row + b.Row);
        public static GridCoord operator -(GridCoord a, GridCoord b) => new GridCoord(a.Col - b.Col, a.Row - b.Row);
        public static GridCoord operator *(GridCoord a, int scalar)  => new GridCoord(a.Col * scalar, a.Row * scalar);
        public static bool operator ==(GridCoord a, GridCoord b) => a.Col == b.Col && a.Row == b.Row;
        public static bool operator !=(GridCoord a, GridCoord b) => !(a == b);

        // --- Equality ---
        public bool Equals(GridCoord other) => this == other;
        public override bool Equals(object obj) => obj is GridCoord g && Equals(g);
        public override int GetHashCode() => (Col * 397) ^ Row;
        public override string ToString() => $"({Col},{Row})";

        // --- Helpers ---
        public bool IsCardinallyAdjacentTo(GridCoord other)
        {
            int dc = Math.Abs(Col - other.Col);
            int dr = Math.Abs(Row - other.Row);
            return (dc == 1 && dr == 0) || (dc == 0 && dr == 1);
        }

        /// <summary>Returns the unit direction from this coord toward <paramref name="target"/>, or Zero if not cardinal.</summary>
        public GridCoord DirectionTo(GridCoord target)
        {
            int dc = target.Col - Col;
            int dr = target.Row - Row;
            if (Math.Abs(dc) == 1 && dr == 0) return new GridCoord(dc, 0);
            if (dc == 0 && Math.Abs(dr) == 1) return new GridCoord(0, dr);
            return Zero;
        }

        /// <summary>True if this coord is a unit-length cardinal direction vector.</summary>
        public bool IsCardinalUnit =>
            (Math.Abs(Col) == 1 && Row == 0) || (Col == 0 && Math.Abs(Row) == 1);
    }
}
