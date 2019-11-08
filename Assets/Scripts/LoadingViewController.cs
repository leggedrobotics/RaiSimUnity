using System;
using UnityEngine;
using UnityEngine.UI;

namespace raisimUnity
{
    public class LoadingViewController : MonoBehaviour
    {
        private GameObject _progressBar;
        private GameObject _title;
        private GameObject _message;
        
        private float _anchorXMin = 0;
        private float _anchorXMax = 0;
        void Awake()
        {
            _progressBar = GameObject.Find("_FillbarForeground");
            _title = GameObject.Find("_TextLoadingTitle");
            _message = GameObject.Find("_TextErrorMessage");
            
            Vector2 anchorMax = _progressBar.GetComponent<RectTransform>().anchorMax;
            Vector2 anchorMin = _progressBar.GetComponent<RectTransform>().anchorMin;
            
            _anchorXMax = anchorMax.x;
            _anchorXMin = anchorMin.x;
            
            _progressBar.GetComponent<RectTransform>().anchorMax = new Vector2(_anchorXMin, 0);
        }

        void Start()
        {
            gameObject.GetComponent<Canvas>().enabled = false;
        }

        public void SetTitle(string title)
        {
            _title.GetComponent<Text>().text = title;
        }

        public void SetMessage(string message)
        {
            _message.GetComponent<Text>().text = message;
        }

        public void SetProgress(float ratio)
        {
            float progress = Math.Max(Math.Min(1, ratio), 0);
            _progressBar.GetComponent<RectTransform>().anchorMax = new Vector2((_anchorXMax - _anchorXMin) * progress, 0);
        }

        public void Show(bool show)
        {
            gameObject.GetComponent<Canvas>().enabled = show;
        }
    }
}