using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DigrisDungeon
{
    public class BlockView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _rect;

        [SerializeField]
        private Image _imageIcon;

        [SerializeField]
        private Image _imageBase;

        [SerializeField]
        private Image[] _imageFrames;

        private Vector2 CellSize => _rect.sizeDelta;

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
            _imageIcon.rectTransform.sizeDelta = CellSize * (data.IsStrata ? 1f : 0.9f);
        }

        public void SetPosition(int x, int y)
        {
            _rect.anchoredPosition = new Vector2(
                CellSize.x * x,
                CellSize.y * y
            );
        }

        public void SetPosition(Vector2Int index)
        {
            SetPosition(index.x, index.y);
        }
    }
}
