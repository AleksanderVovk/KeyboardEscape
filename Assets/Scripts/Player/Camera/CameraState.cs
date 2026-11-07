using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraState : MonoBehaviour
{
    [SerializeField] private GameObject _mainCamera;
    [SerializeField] private GameObject _clothCamera;
    [SerializeField] private GameObject _faceCamera;
    
    public void ChangeState(int state) // 0 - cloth, // 1 - face
    {
        switch (state)
        {
            case 0:
                _mainCamera.gameObject.SetActive(false);
                _clothCamera.SetActive(true);
                _faceCamera.SetActive(false);
                break;
            case 1:
                _mainCamera.gameObject.SetActive(false);
                _clothCamera.SetActive(false);
                _faceCamera.SetActive(true);
                break;
        }
    }

    public void ClearState()
    {
        _mainCamera.gameObject.SetActive(true);
        _clothCamera.SetActive(false);
        _faceCamera.SetActive(false);
    }
}
