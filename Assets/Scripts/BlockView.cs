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

        [SerializeField]
        private RectTransform _rect;

        [SerializeField]
        private Image _imageIcon;

        [SerializeField]
        private Image _imageBase;

        [SerializeField]
        private Image[] _imageFrames;

        public void SetData(Block data)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            _imageIcon.enabled = true;
            _imageBase.enabled = false;
            foreach (var frame in _imageFrames) frame.enabled = false;

            _imageIcon.color = data.IsStrata ? new Color32(255, 150, 100, 255) : Color.white;
            _imageIcon.rectTransform.sizeDelta = CELL_SIZE * (data.IsStrata ? 1f : 0.9f);

            if(data.IsStrata || data.IsMino())
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

        public void SetPosition(Vector2Int index)
        {
            SetPosition(index.x, index.y);
        }
    }
}
