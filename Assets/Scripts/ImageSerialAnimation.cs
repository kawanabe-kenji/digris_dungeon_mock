using UnityEngine;
using UnityEngine.UI;

namespace DigrisDungeon
{
    [RequireComponent(typeof(Image))]
    public class ImageSerialAnimation : MonoBehaviour
    {
        private enum StopAntionType
        {
            None,
            Disable,
            Destroy,
            Loop,
        }

        [SerializeField]
        private Sprite[] _sprites;

        [SerializeField, Range(1, 60)]
        private int _fps = 30;

        [SerializeField]
        private StopAntionType _stopAction;

        private Image _image;

        private int _targetFrameRate;

        private float _frameCount;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _targetFrameRate = Application.targetFrameRate;
            _targetFrameRate = _targetFrameRate <= 0 ? 60 : _targetFrameRate;
            Play();
        }

        private void Update()
        {
            _frameCount += (float)_fps / _targetFrameRate;

            if((int)_frameCount < _sprites.Length)
            {
                _image.enabled = true;
                _image.sprite = _sprites[(int)_frameCount];
                return;
            }

            switch(_stopAction)
            {
                case StopAntionType.None: enabled = false; break;
                case StopAntionType.Disable: gameObject.SetActive(false); break;
                case StopAntionType.Destroy: Destroy(gameObject); break;
                case StopAntionType.Loop: Play(); break;
            }
        }

        public void Play()
        {
            _image.enabled = false;
            _frameCount = 0f;
            if(!enabled) enabled = true;
            if(!gameObject.activeSelf) gameObject.SetActive(true);
        }
    }
}
