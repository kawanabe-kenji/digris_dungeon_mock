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
        }

        public void SetPosition(int x, int y)
        {
            Vector2 cellSize = _rect.sizeDelta;
            _rect.anchoredPosition = new Vector2(
                cellSize.x * x,
                cellSize.y * y
            );
        }

        public void SetPosition(Vector2Int index)
        {
            SetPosition(index.x, index.y);
        }
    }
}
