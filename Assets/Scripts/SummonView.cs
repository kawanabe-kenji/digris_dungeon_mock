using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DigrisDungeon
{
    public class SummonView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _rect;
        public RectTransform Rect => _rect;

        [SerializeField]
        private Image _image;

        public void SetData(Summon data)
        {
            _image.sprite = Resources.Load<Sprite>(string.Format("Sprites/Summon/summon{0:D2}", data.Id));
        }
    }
}
