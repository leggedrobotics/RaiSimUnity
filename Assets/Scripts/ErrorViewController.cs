/*
 * MIT License
 * 
 * Copyright (c) 2019, Dongho Kang, Robotics Systems Lab, ETH Zurich
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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