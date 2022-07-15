using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DigrisDungeon
{
    public class GameManager : MonoBehaviour
    {
        private static readonly Vector2Int MINO_SPAWN_POS = new Vector2Int(5, 14);

        [SerializeField]
        private ControlManager _controlMgr;

        [SerializeField]
        private Transform _blockViewParent;

        [SerializeField]
        private BlockView _blockViewPrefab;

        [SerializeField]
        private Vector2Int _boardSize = new Vector2Int(10, 17);
        public Vector2Int BoardSize => _boardSize;

        private Block[,] _board;

        private BlockView[,] _boardView;

        private Mino _mino;

        private void Awake()
        {
            _board = new Block[BoardSize.x, BoardSize.y];
            _boardView = new BlockView[BoardSize.x, BoardSize.y];
            for (int y = 0; y < BoardSize.y; y++)
            {
                for (int x = 0; x < BoardSize.x; x++)
                {
                    BlockView blockView = Instantiate(_blockViewPrefab, _blockViewParent);
                    _boardView[x, y] = blockView;
                    blockView.SetPosition(x, y);
                }
            }

            _mino = Mino.Create(Mino.RandomShapeType());
            _mino.BoardPos = MINO_SPAWN_POS;
            RefreshBoard();

            InitControl();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) Reset();
        }

        private void Reset()
        {
            SceneManager.LoadScene(0);
        }

        private void InitControl()
        {
            _controlMgr.OnSlideSideEvent = isRight =>
            {
                Vector2Int boardPos = _mino.BoardPos + (isRight ? Vector2Int.right : Vector2Int.left);
                if (CanPutMino(boardPos))
                {
                    _mino.BoardPos = boardPos;
                    RefreshBoard();
                }
            };

            _controlMgr.OnSlideDownEvent = () =>
            {
                Vector2Int boardPos = _mino.BoardPos + Vector2Int.down;
                if (CanPutMino(boardPos))
                {
                    _mino.BoardPos = boardPos;
                }
                else
                {
                    PutMino();
                    RespawnMino();
                }
                RefreshBoard();
            };

            _controlMgr.OnFlickDownEvent = () =>
            {
                Vector2Int boardPos = _mino.BoardPos;
                int count = 0;
                do
                {
                    boardPos += Vector2Int.down;
                    count++;
                } while (CanPutMino(boardPos) && count < BoardSize.y);
                _mino.BoardPos = boardPos + Vector2Int.up;

                PutMino();
                RespawnMino();

                RefreshBoard();
            };

            _controlMgr.OnPushEvent = () =>
            {
                int count = 0;
                do
                {
                    _mino.Rotate();
                    count++;
                } while (!CanPutMino(_mino.BoardPos) && count < 4);
                RefreshBoard();
            };
        }

        #region Board Management
        private Block GetBlock(int x, int y)
        {
            if (x < 0 || x >= _board.GetLength(0)) return null;
            if (y < 0 || y >= _board.GetLength(1)) return null;
            return _board[x, y];
        }
        private Block GetBlock(Vector2Int index)
        {
            return GetBlock(index.x, index.y);
        }

        private BlockView GetBlockView(Vector2Int index)
        {
            if (index.x < 0 || index.x >= _boardView.GetLength(0)) return null;
            if (index.y < 0 || index.y >= _boardView.GetLength(1)) return null;
            return _boardView[index.x, index.y];
        }

        private Vector2Int GetIndex(Block block)
        {
            if (block == null) return -Vector2Int.one;
            for (int y = 0; y < BoardSize.y; y++)
            {
                for (int x = 0; x < BoardSize.x; x++)
                {
                    if (_board[x, y] == block) return new Vector2Int(x, y);
                }
            }
            return -Vector2Int.one;
        }

        private void ExecuteAllBlocks(Action<Block> action)
        {
            for (int y = 0; y < BoardSize.y; y++)
            {
                for (int x = 0; x < BoardSize.x; x++)
                {
                    action.Invoke(_board[x, y]);
                }
            }
        }

        private List<int> GetAlignLines()
        {
            List<int> alignLines = new List<int>();
            for (int y = 0; y < BoardSize.y; y++)
            {
                int xCount = 0;
                for (int x = 0; x < BoardSize.x; x++)
                {
                    Block block = _board[x, y];
                    if (block == null) continue;
                    xCount++;
                }
                if (xCount == BoardSize.x) alignLines.Add(y);
            }
            return alignLines;
        }
        #endregion // Board Management

        private void RefreshBoard()
        {
            List<int> alignLines = GetAlignLines();
            for(int i = alignLines.Count - 1; i >= 0; i--)
            {
                for (int y = alignLines[i]; y < BoardSize.y; y++)
                for (int x = 0; x < BoardSize.x; x++)
                {
                    _board[x, y] = GetBlock(x, y + 1);
                }
            }

            for (int y = 0; y < BoardSize.y; y++)
            {
                for (int x = 0; x < BoardSize.x; x++)
                {
                    _boardView[x, y].SetData(_board[x, y]);
                }
            }

            foreach(var kvp in _mino.Blocks)
            {
                Vector2Int offset = kvp.Key;
                BlockView view = GetBlockView(_mino.BoardPos + offset);
                if (view == null) continue;
                Block data = kvp.Value;
                view.SetData(data);
            }
        }

        private bool CanPutMino(Vector2Int boardPos)
        {
            foreach (var kvp in _mino.Blocks)
            {
                Vector2Int offset = kvp.Key;
                Vector2Int index = boardPos + offset;
                // 指定したミノを指定した位置に置いたとき、壁や地面にぶつかっているかどうか
                if (index.x < 0 || index.y < 0 || index.x >= BoardSize.x) return false;
                // 指定したミノを指定した位置に置いたとき、いずれかのブロックが設置済みブロックにぶつかっているかどうか
                if (GetBlock(index) != null) return false;
            }
            return true;

        }

        private void PutMino()
        {
            foreach (var kvp in _mino.Blocks)
            {
                Vector2Int index = _mino.BoardPos + kvp.Key;
                _board[index.x, index.y] = kvp.Value;
            }
        }

        private void RespawnMino()
        {
            _mino.Respawn(Mino.RandomShapeType());
            _mino.BoardPos = MINO_SPAWN_POS;

            // FIXME: ミノが上に詰まった時
            if (!CanPutMino(MINO_SPAWN_POS))
            {
                Reset();
            }
        }
    }
}
