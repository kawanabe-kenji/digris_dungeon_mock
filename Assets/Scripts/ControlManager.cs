using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DigrisDungeon
{
    public class ControlManager : MonoBehaviour, IDragHandler, IPointerClickHandler
    {
        public Action<bool> OnSlideSideEvent;

        public Action OnSlideDownEvent;

        public Action OnFlickDownEvent;

        public Action OnFlickUpEvent;

        public Action OnPushEvent;

        [SerializeField]
        private Image _raycastTarget;

        public bool Interactable
        {
            set
            {
                enabled = value;
                _raycastTarget.raycastTarget = value;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Debug.Log("OnDrag");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("OnPointerClick");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                FlickUp();
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SlideSide(false);
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                SlideDown();
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                SlideSide(true);
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                FlickDown();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                Push();
            }
        }

        private void SlideSide(bool isRight)
        {
            // TODO: 左右移動
            OnSlideSideEvent?.Invoke(isRight);
        }

        private void SlideDown()
        {
            // TODO: 高速落下
            OnSlideDownEvent?.Invoke();
        }

        private void FlickDown()
        {
            // TODO: ハードドロップ
            OnFlickDownEvent?.Invoke();
        }

        private void FlickUp()
        {
            // TODO: ストック
            OnFlickUpEvent?.Invoke();
        }

        private void Push()
        {
            // TODO: 回転
            OnPushEvent?.Invoke();
        }
    }
}
