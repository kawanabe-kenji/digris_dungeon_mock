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
        public static GameManager Instance { get; private set; }

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
        private Transform _summonViewParent;

        [SerializeField]
        private SummonView _summonViewPrefab;

        [SerializeField]
        private Vector2Int _boardSize = new Vector2Int(10, 17);
        public Vector2Int BoardSize => _boardSize;

        private Block[,] _board;

        private BlockView[,] _boardView;

        private Mino _mino;

        private Dictionary<Summon, SummonView> _summons = new Dictionary<Summon, SummonView>();

        public static int Distance(Vector2Int a, Vector2Int b)
        {
            return Math.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private void Awake()
        {
            Instance = this;

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
        private Vector2 GetBoardPosition(Vector2Int index)
        {
            return GetBoardPosition(index.x, index.y);
        }

        private Vector2 GetBoardAnchoredPosition(int x, int y)
        {
            return new Vector2(
                BlockView.CELL_SIZE.x * x,
                BlockView.CELL_SIZE.y * y
            );
        }
        private Vector2 GetBoardAnchoredPosition(Vector2Int index)
        {
            return GetBoardAnchoredPosition(index.x, index.y);
        }

        private int GetHighestStrata()
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
            return highestStrata;
        }
        #endregion // Board Management

        #region Summon
        private SummonView CreateSummonView(Summon data)
        {
            SummonView view = Instantiate(_summonViewPrefab, _summonViewParent);
            view.SetData(data);
            _summons.Add(data, view);
            return view;
        }

        public SummonView GetSummonView(Summon data)
        {
            if(_summons.TryGetValue(data, out SummonView view)) return view;
            return CreateSummonView(data);
        }

        private void DestroySummonView(Summon data)
        {
            if(data == null) return;
            Debug.Log("DestroySummonView");
            SummonView view = GetSummonView(data);
            if(view == null) return;
            _summons.Remove(data);
            Destroy(view.gameObject);
        }
        #endregion // Summon

        private void RefreshBoard()
        {
            DrawBoard();
            _controlMgr.Interactable = false;

            Sequence seq = DOTween.Sequence();

            // ラインが揃ってる列を崩す
            BrakeAlignLines(seq);

            // パーティモンスターがルートを歩く
            TraceSummon(seq);

            seq.AppendCallback(() =>
            {
                Sequence seqDrop = DOTween.Sequence();
                // 単一ブロックを落とす
                bool isDropped = DropSingleBlock(seqDrop);
                if (isDropped)
                {
                    // ラインが揃ってる列を空けて下に詰める
                    EraseAlignLines(seqDrop);
                    // 必要な深さだけ画面をスクロールして地層を露出させる
                    if (GetHighestStrata() < 0) ScrollStrata(seqDrop);
                    seqDrop.AppendCallback(RefreshBoard);
                }
                else
                {
                    ScrollStrata(seqDrop);
                    seqDrop.AppendCallback(() =>
                    {
                        // 盤面データをビューに反映
                        DrawBoard();
                        DrawMino();
                        _controlMgr.Interactable = true;
                    });
                }
            });
        }

        /// <summary>
        /// ラインが揃ってる列を崩す
        /// </summary>
        private void BrakeAlignLines(Sequence seq)
        {
            bool isBrake = false;
            List<int> alignLines = GetAlignLines();
            for (int i = alignLines.Count - 1; i >= 0; i--)
            {
                int y = alignLines[i];
                for (int x = 0; x < BoardSize.x; x++)
                {
                    Block brokenBlock = _board[x, y];
                    if (brokenBlock == null) continue;
                    // 対象ブロックと連結するブロックから、連結を外す
                    foreach(Block linckedBlock in brokenBlock.LinkedBlocks)
                        linckedBlock.LinkedBlocks.Remove(brokenBlock);
                    // 対象ブロック自身から全ての連結を外す
                    brokenBlock.LinkedBlocks.Clear();
                    // 地層ブロックフラグを外す
                    brokenBlock.IsStrata = false;
                    // 召喚キャラを削除する TODO: スキルを発動させる
                    Summon eraseSummon = brokenBlock.Summon;
                    brokenBlock.Summon = null;

                    BlockView blockView = GetBlockView(x, y);
                    Vector2 boardPos = GetBoardPosition(x, y);
                    seq.AppendCallback(() => {
                        // 召喚キャラを削除する TODO: スキルを発動させる
                        DestroySummonView(eraseSummon);

                        blockView.SetData(brokenBlock);
                        foreach (Block linckedBlock in brokenBlock.LinkedBlocks)
                            GetBlockView(GetIndex(linckedBlock)).SetData(linckedBlock);
                        RectTransform effectPrefab = Resources.Load<RectTransform>("Effects/ef_stone_impact/ef_stone_impact_rect");
                        RectTransform effect = Instantiate(effectPrefab, _effectsParent);
                        effect.anchoredPosition = boardPos;
                    });
                    isBrake = true;
                }
            }
            if (isBrake) seq.AppendInterval(0.5f);
        }

        /// <summary>
        /// 単一ブロックを落とす
        /// </summary>
        /// <returns>ドロップが発生したかどうか</returns>
        private bool DropSingleBlock(Sequence seq)
        {
            bool isDropped = false;
            Action preDropEvent = null;
            for(int y = 0; y < BoardSize.y; y++)
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

                    BlockView blockView = GetBlockView(x, dropY);
                    BlockView preBlockView = GetBlockView(x, y);
                    int preY = y;
                    preDropEvent += () => {
                        blockView.SetData(block);
                        blockView.SetPositionY(preY);
                        preBlockView.SetData(null);
                    };
                    Tween tween = blockView.Rect.DOAnchorPosY(BlockView.CELL_SIZE.y * dropY, 0.2f * (preY - dropY)).SetEase(Ease.OutCubic);
                    if (isDropped) seq.Join(tween);
                    else
                    {
                        seq.AppendCallback(() => preDropEvent?.Invoke());
                        seq.Append(tween);
                    }
                    isDropped = true;
                }
            }
            //if(isDropped) seq.AppendInterval(0.2f);
            return isDropped;
        }

        private bool DropBlock(Sequence seq)
        {
            bool isDropped = false;
            for (int y = 0; y < BoardSize.y; y++)
            {
                for (int x = 0; x < BoardSize.x; x++)
                {
                    Block block = _board[x, y];
                    if (block == null || block.IsStrata) continue;
                }
            }
            return isDropped;
        }

        /// <summary>
        /// ラインが揃ってる列を空けて下に詰める
        /// </summary>
        private void EraseAlignLines(Sequence seq)
        {
            bool isFirst = true;
            Action preEraseEvent = null;
            List<int> alignLines = GetAlignLines();
            for (int i = alignLines.Count - 1; i >= 0; i--)
            {
                for (int y = alignLines[i]; y < BoardSize.y; y++)
                {
                    for (int x = 0; x < BoardSize.x; x++)
                    {
                        Block erasedBlock = _board[x, y];
                        bool isEraseLine = y == alignLines[i];
                        Summon eraseSummon = null;
                        if (isEraseLine && erasedBlock != null)
                        {
                            // 消す対象のブロックと連結するブロックから、連結を外す
                            foreach (Block linckedBlock in erasedBlock.LinkedBlocks)
                                linckedBlock.LinkedBlocks.Remove(erasedBlock);

                            // 召喚キャラを削除する TODO: スキルを発動させる
                            eraseSummon = erasedBlock.Summon;
                            erasedBlock.Summon = null;
                        }
                        int dropY = y + 1;
                        Block dropBlock = GetBlock(x, dropY);
                        _board[x, y] = dropBlock;

                        BlockView blockView = GetBlockView(x, y);
                        Vector2 boardPos = GetBoardPosition(x, y);
                        preEraseEvent += () => {
                            // 召喚キャラを削除する TODO: スキルを発動させる
                            DestroySummonView(eraseSummon);

                            blockView.SetData(dropBlock);
                            if (dropBlock != null) blockView.SetPositionY(dropY);
                            if (isEraseLine)
                            {
                                RectTransform effectPrefab = Resources.Load<RectTransform>("Effects/ef_impact/ef_impact_rect");
                                RectTransform effect = Instantiate(effectPrefab, _effectsParent);
                                effect.anchoredPosition = boardPos;
                            }
                        };

                        if(dropBlock == null) continue;

                        Tween tween = blockView.Rect.DOAnchorPosY(BlockView.CELL_SIZE.y * y, 0.2f).SetEase(Ease.OutCubic);
                        if(isFirst)
                        {
                            seq.AppendCallback(() => preEraseEvent?.Invoke());
                            seq.AppendInterval(0.2f);
                            seq.Append(tween);
                        }
                        else seq.Join(tween);
                        isFirst = false;
                    }
                }
            }
            if (alignLines.Count > 0) seq.AppendInterval(0.1f);
        }

        /// <summary>
        /// 必要な深さだけ画面をスクロールして地層を露出させる
        /// </summary>
        /// <returns>スクロールしたか否か</returns>
        private void ScrollStrata(Sequence seq)
        {
            int highestStrata = GetHighestStrata();
            if (highestStrata >= MIN_HEIGHT_STRATA) return;

            int diff = MIN_HEIGHT_STRATA - highestStrata;
            for (int y = BoardSize.y - 1; y >= 0; y--)
            {
                int ty = y - diff;
                int emptyX = UnityEngine.Random.Range(0, BoardSize.x);
                for (int x = 0; x < BoardSize.x; x++)
                {
                    // TODO: 召喚キャラがスクロールアウトされたらビューを削除する

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
            //seq.AppendInterval(0.3f);
            seq.Append(_blockViewParent.DOAnchorPosY(0f, 0.2f * diff).SetEase(Ease.OutCubic));
        }

        /// <summary>
        /// パーティモンスターがルートを歩く
        /// </summary>
        private void TraceSummon(Sequence seq)
        {
            Dictionary<Vector2Int, Summon> summons = new Dictionary<Vector2Int, Summon>();
            for(int y = 0; y < BoardSize.y; y++)
            for(int x = 0; x < BoardSize.x; x++)
            {
                Block block = _board[x, y];
                if (block == null) continue;
                Summon summon = block.Summon;
                if(summon != null) summons.Add(new Vector2Int(x, y), summon);
            }

            DirectionType[] directions = new DirectionType[] { DirectionType.Right, DirectionType.Left, DirectionType.Down };
            foreach(var kvp in summons)
            {
                Vector2Int pos = kvp.Key;
                Summon summon = kvp.Value;

                Block startBlock = GetBlock(pos);

                bool isOneMove = false;
                bool isMove = false;
                do
                {
                    isMove = false;
                    for (int i = 0; i < directions.Length; i++)
                    {
                        var movePos = pos + directions[i].GetOffset();
                        Block moveBlock = GetBlock(movePos);
                        if (moveBlock == null || !moveBlock.IsRounte) continue;

                        // 歩いたルートブロックを消費
                        _board[pos.x, pos.y] = null;

                        // 隣接ブロックを崩す
                        for (int j = 0; j < (int)DirectionType.Max; j++)
                        {
                            Vector2Int offset = ((DirectionType)j).GetOffset();
                            var breakBlock = GetBlock(pos + offset);
                            if (breakBlock == null || breakBlock.IsRounte || breakBlock.Summon != null) continue;
                            // 対象ブロックと連結するブロックから、連結を外す
                            foreach (Block linckedBlock in breakBlock.LinkedBlocks)
                                linckedBlock.LinkedBlocks.Remove(breakBlock);
                            // 対象ブロック自身から全ての連結を外す
                            breakBlock.LinkedBlocks.Clear();
                        }

                        // 歩いたルートブロックを非表示にする
                        BlockView eraseBlock = GetBlockView(pos);
                        // 隣接ブロックを崩す演出
                        var summonView = GetSummonView(summon);
                        var currentPos = pos;
                        seq.AppendCallback(() => {
                            eraseBlock.SetData(null);
                            for (int j = 0; j < (int)DirectionType.Max; j++)
                            {
                                Vector2Int brakePos = currentPos + ((DirectionType)j).GetOffset();
                                var targetBlock = GetBlock(brakePos);
                                if (targetBlock == null || targetBlock.IsRounte || targetBlock.Summon != null) continue;
                                GetBlockView(brakePos).SetData(targetBlock);
                                RectTransform effectPrefab = Resources.Load<RectTransform>("Effects/ef_stone_impact/ef_stone_impact_rect");
                                RectTransform effect = Instantiate(effectPrefab, _effectsParent);
                                effect.anchoredPosition = GetBoardPosition(brakePos);
                            }
                            summonView.Rect.SetParent(_summonViewParent, true);
                        });
                        // 召喚キャラの移動演出
                        var rectPos = GetBoardAnchoredPosition(movePos);
                        seq.Append(summonView.Rect.DOAnchorPos(rectPos, 0.5f));

                        pos = movePos;
                        isOneMove = true;
                        isMove = true;
                        break;
                    }
                } while (isMove);

                if (isOneMove)
                {
                    // 隣接ブロックを崩す
                    for (int j = 0; j < (int)DirectionType.Max; j++)
                    {
                        Vector2Int offset = ((DirectionType)j).GetOffset();
                        var breakBlock = GetBlock(pos + offset);
                        if (breakBlock == null || breakBlock.IsRounte || breakBlock.Summon != null) continue;
                        // 対象ブロックと連結するブロックから、連結を外す
                        foreach (Block linckedBlock in breakBlock.LinkedBlocks)
                            linckedBlock.LinkedBlocks.Remove(breakBlock);
                        // 対象ブロック自身から全ての連結を外す
                        breakBlock.LinkedBlocks.Clear();
                    }

                    _board[pos.x, pos.y] = null;

                    startBlock.Summon = null;
                    seq.AppendCallback(() => {
                        for (int j = 0; j < (int)DirectionType.Max; j++)
                        {
                            Vector2Int brakePos = pos + ((DirectionType)j).GetOffset();
                            var targetBlock = GetBlock(brakePos);
                            if (targetBlock == null || targetBlock.IsRounte || targetBlock.Summon != null) continue;
                            GetBlockView(brakePos).SetData(targetBlock);
                            RectTransform effectPrefab = Resources.Load<RectTransform>("Effects/ef_stone_impact/ef_stone_impact_rect");
                            RectTransform effect = Instantiate(effectPrefab, _effectsParent);
                            effect.anchoredPosition = GetBoardPosition(brakePos);
                        }
                        GetBlockView(pos).SetData(null);
                        DestroySummonView(summon);
                    });
                    seq.AppendInterval(0.5f);
                }
            }
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
