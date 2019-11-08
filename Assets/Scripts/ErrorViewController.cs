using UnityEngine;
using UnityEngine.UI;

namespace raisimUnity
{
    public class ErrorViewController : MonoBehaviour
    {
        private GameObject _title;
        private GameObject _message;
        
        void Awake()
        {
            _title = GameObject.Find("_TextErrorTitle");
            _message = GameObject.Find("_TextErrorMessage");
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
        
        public void Show(bool show)
        {
            gameObject.GetComponent<Canvas>().enabled = show;
        }
    }
}