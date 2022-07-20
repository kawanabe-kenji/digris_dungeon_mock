using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace DigrisDungeon
{
    public class GameManager : MonoBehaviour
    {
        private static readonly Vector2Int MINO_SPAWN_POS = new Vector2Int(5, 14);

        /// <summary> 地層の最低高さ </summary>
        private const int MIN_HEIGHT_STRATA = 4;

        [SerializeField]
        private ControlManager _controlMgr;

        [SerializeField]
        private RectTransform _blockViewParent;

        [SerializeField]
        private BlockView _blockViewPrefab;

        [SerializeField]
        private Transform _effectsParent;

        [SerializeField]
        private Vector2Int _boardSize = new Vector2Int(10, 17);
        public Vector2Int BoardSize => _boardSize;

        private Block[,] _board;

        private BlockView[,] _boardView;

        private Mino _mino;

        public static int Distance(Vector2Int a, Vector2Int b)
        {
            return Math.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

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

            InitControl();

            _controlMgr.Interactable = false;
            Sequence seq = DOTween.Sequence();
            ScrollStrata(seq);
            seq.AppendCallback(() => {
                DrawMino();
                _controlMgr.Interactable = true;
            });
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
                    DrawBoard();
                    DrawMino();
                }
            };

            _controlMgr.OnSlideDownEvent = () =>
            {
                Vector2Int boardPos = _mino.BoardPos + Vector2Int.down;
                if (CanPutMino(boardPos))
                {
                    _mino.BoardPos = boardPos;
                    DrawBoard();
                    DrawMino();
                }
                else
                {
                    PutMino();
                    RespawnMino();
                    RefreshBoard();
                }
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
                DrawBoard();
                DrawMino();
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

        private BlockView GetBlockView(int x, int y)
        {
            if (x < 0 || x >= _boardView.GetLength(0)) return null;
            if (y < 0 || y >= _boardView.GetLength(1)) return null;
            return _boardView[x, y];
        }
        private BlockView GetBlockView(Vector2Int index)
        {
            return GetBlockView(index.x, index.y);
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

        private Vector2 GetBoardPosition(int x, int y)
        {
            return new Vector2(
                BlockView.CELL_SIZE.x * (0.5f + x),
                BlockView.CELL_SIZE.y * (0.5f + y)
            );
        }
        #endregion // Board Management

        private void RefreshBoard()
        {
            _controlMgr.Interactable = false;

            Sequence seq = DOTween.Sequence();

            // ラインが揃ってる列を崩す
            BrakeAlignLines(seq);

            bool isDropped;
            do
            {
                // 単一ブロックを落とす
                isDropped = DropSingleBlock(seq);
                // ラインが揃ってる列を空けて下に詰める
                EraseAlignLines(seq);
                // 必要な深さだけ画面をスクロールして地層を露出させる
                ScrollStrata(seq);
            } while (isDropped);

            seq.AppendCallback(() => {
                // 盤面データをビューに反映
                DrawBoard();
                DrawMino();
                _controlMgr.Interactable = true;
            });
        }

        /// <summary>
        /// ラインが揃ってる列を崩す
        /// </summary>
        private void BrakeAlignLines(Sequence seq)
        {
            List<int> alignLines = GetAlignLines();
            for (int i = alignLines.Count - 1; i >= 0; i--)
            {
                int y = alignLines[i];
                for (int x = 0; x < BoardSize.x; x++)
                {
                    Block brokenBlock = _board[x, y];
                    if (brokenBlock == null) continue;
                    // 対象ブロックと連結するブロックから、連結を外す
                    foreach (Block linckedBlock in brokenBlock.LinkedBlocks)
                        linckedBlock.LinkedBlocks.Remove(brokenBlock);
                    // 対象ブロック自身から全ての連結を外す
                    brokenBlock.LinkedBlocks.Clear();
                    // 地層ブロックフラグを外す
                    brokenBlock.IsStrata = false;

                    Vector2 boardPos = GetBoardPosition(x, y);
                    seq.AppendCallback(() => {
                        RectTransform effectPrefab = Resources.Load<RectTransform>("Effects/ef_stone_impact/ef_stone_impact_rect");
                        RectTransform effect = Instantiate(effectPrefab, _effectsParent);
                        effect.anchoredPosition = boardPos;
                    });
                }
            }
            seq.AppendCallback(DrawBoard);
            seq.AppendInterval(0.5f);
        }

        /// <summary>
        /// 単一ブロックを落とす
        /// </summary>
        /// <returns>ドロップが発生したかどうか</returns>
        private bool DropSingleBlock(Sequence seq)
        {
            seq.AppendCallback(DrawBoard);
            bool isDropped = false;
            for (int y = 0; y < BoardSize.y; y++)
            {
                for (int x = 0; x < BoardSize.x; x++)
                {
                    Block block = _board[x, y];
                    if (block == null || block.IsStrata || block.IsMino()) continue;
                    int dropY = y;
                    for (int ty = y - 1; ty >= 0; ty--)
                    {
                        if (_board[x, ty] != null) break;
                        dropY = ty;
                    }
                    if (dropY == y) continue;
                    _board[x, dropY] = block;
                    _board[x, y] = null;
                    isDropped = true;

                    BlockView blockView = GetBlockView(x, dropY);
                    BlockView preBlockView = GetBlockView(x, y);
                    int preY = y;
                    seq.PrependCallback(() => {
                        blockView.SetData(block);
                        blockView.SetPositionY(preY);
                        preBlockView.SetData(null);
                    });
                    seq.Join(blockView.Rect.DOAnchorPosY(BlockView.CELL_SIZE.y * dropY, 1f).SetEase(Ease.OutCubic));
                }
            }
            if(isDropped) seq.AppendInterval(0.2f);
            return isDropped;
        }

        /// <summary>
        /// ラインが揃ってる列を空けて下に詰める
        /// </summary>
        private void EraseAlignLines(Sequence seq)
        {
            seq.AppendCallback(DrawBoard);
            List<int> alignLines = GetAlignLines();
            for (int i = alignLines.Count - 1; i >= 0; i--)
            {
                for (int y = alignLines[i]; y < BoardSize.y; y++)
                {
                    for (int x = 0; x < BoardSize.x; x++)
                    {
                        Block erasedBlock = _board[x, y];
                        bool isEraseLine = y == alignLines[i];
                        if (isEraseLine && erasedBlock != null)
                        {
                            // 消す対象のブロックと連結するブロックから、連結を外す
                            foreach (Block linckedBlock in erasedBlock.LinkedBlocks)
                                linckedBlock.LinkedBlocks.Remove(erasedBlock);
                        }
                        int dropY = y + 1;
                        Block dropBlock = GetBlock(x, dropY);
                        _board[x, y] = dropBlock;

                        BlockView blockView = GetBlockView(x, y);
                        Vector2 boardPos = GetBoardPosition(x, y);
                        seq.PrependCallback(() => {
                            blockView.SetData(dropBlock);
                            if (isEraseLine && dropBlock != null)
                            {
                                blockView.SetPositionY(dropY);
                                RectTransform effectPrefab = Resources.Load<RectTransform>("Effects/ef_impact/ef_impact_rect");
                                RectTransform effect = Instantiate(effectPrefab, _effectsParent);
                                effect.anchoredPosition = boardPos;
                            }
                        });
                        if (dropBlock != null)
                        {
                            int ty = y;
                            seq.Join(blockView.Rect.DOAnchorPosY(BlockView.CELL_SIZE.y * ty, 1f).SetEase(Ease.OutCubic));
                        }
                    }
                }
            }
            if (alignLines.Count > 0) seq.AppendInterval(0.2f);
        }

        /// <summary>
        /// 必要な深さだけ画面をスクロールして地層を露出させる
        /// </summary>
        /// <returns>スクロールしたか否か</returns>
        private void ScrollStrata(Sequence seq)
        {
            int highestStrata = -1;
            for (int y = 0; y < BoardSize.y; y++)
            for (int x = 0; x < BoardSize.x; x++)
            {
                if (_board[x, y] != null && _board[x, y].IsStrata)
                {
                    highestStrata = y;
                    break;
                }
            }

            if (highestStrata >= MIN_HEIGHT_STRATA) return;

            int diff = MIN_HEIGHT_STRATA - highestStrata;
            for (int y = BoardSize.y - 1; y >= 0; y--)
            {
                int ty = y - diff;
                int emptyX = UnityEngine.Random.Range(0, BoardSize.x);
                for (int x = 0; x < BoardSize.x; x++)
                {
                    if (ty >= 0)
                        _board[x, y] = _board[x, ty];
                    else if (x != emptyX)
                    {
                        _board[x, y] = new Block();
                        _board[x, y].IsStrata = true;
                    }
                    else
                        _board[x, y] = null;
                }
            }

            // 盤面スクロールの演出
            seq.AppendCallback(() =>
            {
                DrawBoard();
                _blockViewParent.anchoredPosition = Vector2.down * BlockView.CELL_SIZE.y * diff;
            });
            seq.AppendInterval(0.3f);
            seq.Append(_blockViewParent.DOAnchorPosY(0f, 0.2f * diff).SetEase(Ease.OutCubic));
        }

        private void DrawBoard()
        {
            for (int y = 0; y < BoardSize.y; y++)
            {
                for (int x = 0; x < BoardSize.x; x++)
                {
                    _boardView[x, y].SetData(_board[x, y]);
                }
            }
        }

        private void DrawMino()
        {
            foreach (var kvp in _mino.Blocks)
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
