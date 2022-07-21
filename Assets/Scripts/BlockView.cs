using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DigrisDungeon
{
    public class BlockView : MonoBehaviour
    {
        public static readonly Vector2 CELL_SIZE = new Vector2(100f, 100f);

        private const string PATH_ICON_BLOCK = "Sprites/Blocks/block_stone";

        private const string PATH_ICON_BLOCK_BROKEN = "Sprites/Blocks/block_stone_brake_masked";

        private const string PATH_ICON_ROUTE = "Sprites/Blocks/energy{0:D2}";

        [SerializeField]
        private RectTransform _rect;
        public RectTransform Rect => _rect;

        [SerializeField]
        private Image _imageIcon;

        [SerializeField]
        private Image _imageBase;

        [SerializeField]
        private Image[] _imageFrames;

        private Block _data;

        private List<Sprite> _animSprites = new List<Sprite>();

        private float _animSpeed;

        public void SetData(Block data)
        {
            _data = data;
            _animSprites.Clear();
            _animSpeed = 1;

            if(_data == null)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            _imageIcon.enabled = true;
            _imageBase.enabled = false;
            foreach(var frame in _imageFrames) frame.enabled = false;

            _imageIcon.color = _data.IsStrata ? new Color32(255, 150, 100, 255) : Color.white;
            _imageIcon.rectTransform.sizeDelta = CELL_SIZE * (_data.IsStrata || data.Summon != null ? 1f : 0.9f);

            if(_data.IsRounte)
            {
                _animSprites.Add(Resources.Load<Sprite>(string.Format(PATH_ICON_ROUTE, 1)));
                _animSprites.Add(Resources.Load<Sprite>(string.Format(PATH_ICON_ROUTE, 2)));
                _animSpeed = 0.05f;
            }
            else if(_data.Summon != null)
            {
                _imageIcon.sprite = Resources.Load<Sprite>("Sprites/frame_r30");
                SummonView summon = GameManager.Instance.GetSummonView(_data.Summon);
                summon.Rect.SetParent(Rect);
                summon.Rect.anchoredPosition = Vector2.zero;
            }
            else if(_data.IsStrata || _data.IsMino())
                _imageIcon.sprite = Resources.Load<Sprite>(PATH_ICON_BLOCK);
            else
                _imageIcon.sprite = Resources.Load<Sprite>(PATH_ICON_BLOCK_BROKEN);

        }

        public void SetPosition(int x, int y)
        {
            _rect.anchoredPosition = new Vector2(
                CELL_SIZE.x * x,
                CELL_SIZE.y * y
            );
        }
        public void SetPositionX(int x)
        {
            Vector2 anchoredPos = _rect.anchoredPosition;
            anchoredPos.x = CELL_SIZE.x * x;
            _rect.anchoredPosition = anchoredPos;
        }
        public void SetPositionY(int y)
        {
            Vector2 anchoredPos = _rect.anchoredPosition;
            anchoredPos.y = CELL_SIZE.y * y;
            _rect.anchoredPosition = anchoredPos;
        }

        public void SetPosition(Vector2Int index)
        {
            SetPosition(index.x, index.y);
        }

        private void Update()
        {
            if(_data == null || !_data.IsRounte) return;
            int frame = (int)(Time.frameCount * _animSpeed) % _animSprites.Count;
            _imageIcon.sprite = _animSprites[frame];
        }
    }
}
