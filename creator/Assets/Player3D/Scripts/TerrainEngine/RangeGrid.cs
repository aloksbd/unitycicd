using System;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainEngine
{
    public class RangeGrid
    {
        // Constants
        public const int INVALID_INDEX = -1;

        // Private members
        private int _rows;
        private int _columns;
        private List<Cell> _grid;
        private Dictionary<int, Range> _ranges;

        // Public types
        public class Cell
        {
            public enum Status
            {
                None = 0,
                ToDownload,
                Downloading,
                Processing,
                Processed,
            };

            public int row;
            public int col;
            public Bounds2D worldBounds;            //  bounds in Unity coordinates
            public Wgs84Bounds wgs84Bounds;         //  bounds in lat/lon  
            public Status status;
            public Dictionary<int, object> tags;    //  tag list
        };

        class Range
        {
            public int   rangeId; // caller-provide range identifier
            public float near;    // near range in meters
            public float far;     // far range in meters
        }

        //  Public methods - cell I/O

        public void InitializeFromArea(
            ref Bounds2D worldAreaBounds,
            ref Wgs84Bounds wgs85AreaBounds,
            int rows,
            int columns)
        {
            Trace.Assert(rows > 0, "RangeGrid.Initialize() invalid argument: rows = {0}", rows);
            Trace.Assert(columns > 0, "RangeGrid.Initialize() invalid argument: columns = {0}", columns);

            double worldSpanX = (worldAreaBounds.right - worldAreaBounds.left) / columns;
            double worldSpanY = (worldAreaBounds.bottom - worldAreaBounds.top) / rows;
            double wgs84spanX = (wgs85AreaBounds.right - wgs85AreaBounds.left) / columns;
            double wgs84spanY = (wgs85AreaBounds.bottom - wgs85AreaBounds.top) / rows;

            _grid = new List<Cell>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cell cell = new Cell()
                    {
                        row = r,
                        col = c,
                        worldBounds = new Bounds2D()
                        {
                            top = worldAreaBounds.top + (worldSpanY * r),
                            left = worldAreaBounds.left + (worldSpanX * c),
                            bottom = worldAreaBounds.top + (worldSpanY * (r + 1)),
                            right = worldAreaBounds.left + (worldSpanX * (c + 1))
                        },
                        wgs84Bounds = new Wgs84Bounds()
                        {
                            top = wgs85AreaBounds.top + (wgs84spanY * r),
                            left = wgs85AreaBounds.left + (wgs84spanX * c),
                            bottom = wgs85AreaBounds.top + (wgs84spanY * (r + 1)),
                            right = wgs85AreaBounds.left + (wgs84spanX * (c + 1))
                        },
                        tags = new Dictionary<int, object>(),
                    };
                    _grid.Add(cell);
                }
            }

            _rows = rows;
            _columns = columns;
            
            Trace.Assert(IndexAt(rows-1, columns-1) == _grid.Count - 1, "RangeGrid.Initialize(): Failed to allocate correct number of cells");
        }

        bool TryGetCell(int row, int col, out Cell cell)
        {
            int index = IndexAt(row, col);
            if (index != INVALID_INDEX)
            {
                cell = _grid[index];
                return true;
            }
            cell = null;
            return false;
        }

        public void ClearGrid()
        {
            _grid = null;
            _rows = 0;
            _columns = 0;
        }

        public void Dispose()
        {
            ClearGrid();
            _ranges = null;
        }

        //  Cell status

        public bool TrySetStatus(int row, int col, Cell.Status status)
        {
            int index = IndexAt(row, col);
            if (index != INVALID_INDEX)
            {
                _grid[index].status = status;
                return true;
            }
            return false;
        }

        public bool TryGetStatus(int row, int col, out Cell.Status status)
        {
            int index = IndexAt(row, col);
            if (index != INVALID_INDEX)
            {
                status = _grid[index].status;
                return true;
            }
            status = Cell.Status.None;
            return false;
        }

        //  Cell tagging

        public bool TryGetTag(int row, int col, int iTag, out object value)
        {
            value = null;
            int index = IndexAt(row, col);
            if (index != INVALID_INDEX)
            {
                return _grid[index].tags.TryGetValue(iTag, out value);
            }
            return false;
        }

        public bool TrySetTag(int row, int col, int iTag, object value)
        {
            int index = IndexAt(row, col);
            if (index != INVALID_INDEX)
            {
                _grid[index].tags[iTag] = value;
                return true;
            }
            return false;
        }

        //  Public methods - distance ranges

        public bool SetRange(int rangeId, float near, float far)
        {
            Trace.Assert(far > near, "DynamiceViewGrid.AddRange(): Far value {0} must be greater than near value {1}", far, near);

            if (_ranges == null)
            {
                _ranges = new Dictionary<int, Range>();
            }
            _ranges[rangeId] = new Range() { rangeId = rangeId, near = near, far = far };
            return true;
        }

        private struct CellDist
        {
            public double distance;
            public Cell cell;
            public static int Compare(CellDist x, CellDist y)
            {
                return x.distance < y.distance ? -1 :
                    x.distance > y.distance ? 1 :
                    0;
            }
        }

        public int GetCellsInRange(Vector3 sourcePosition, int rangeId, out List<Cell> cells)
        {
            cells = null;
            Range range;
            if (!_ranges.TryGetValue(rangeId, out range))
            {
                return 0;
            }

            List<CellDist> cellDists = new List<CellDist>();
            foreach (Cell cell in _grid)
            {
                Vector2d cellCenter2d = cell.worldBounds.Center;
                Vector3 cellCenter = new Vector3((float)cellCenter2d.x, 0, (float)cellCenter2d.y);
                double distance = Vector3.Magnitude(sourcePosition - cellCenter);
                if (distance >= range.near && distance <= range.far)
                {
                    cellDists.Add(new CellDist() { distance = distance, cell = cell });
                }
            }

            if (cellDists.Count > 0)
            {
                cellDists.Sort(CellDist.Compare);
                cells = new List<Cell>();
                foreach (CellDist cd in cellDists)
                {
                    cells.Add(cd.cell);
                }
            }

            return (cells != null) ? cells.Count : 0;
        }

        //  Internal helpers

        private int IndexAt(int row, int col)
        {
            int index = (row * _rows) + col;
            return (_grid != null && index < _grid.Count) ? index : INVALID_INDEX;
        }
    }
}
