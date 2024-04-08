using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;


public class Distance : MonoBehaviour
{
    [SerializeField] private ARRaycastManager _raycastManager;
    [SerializeField] private GameObject _spawnablePrefab;
    [SerializeField] private Camera _arCamera;
    [SerializeField] private TMP_Text distanceText;

    private List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();
    private GameObject _spawnedObject;
    private bool _objectPlaced = false;
    private bool _isDestroying = false;
    private float _initialPinchDistance;
    private Vector3 _initialScale;
    private bool _isPinching = false;

    private int _tapCount = 0;
    private float _lastTapTime = 0;
    private float _multipleTapMaxDelay = 0.5f; // Max delay between taps for it to be considered a multiple tap

    private void Update()
    {
        if (Input.touchCount == 0 || _isDestroying)
        {
            _isPinching = false; // Reset pinch state
            if (!_objectPlaced)
            {
                UpdateContinuousRaycastDistance();
            }
            return;
        }

        if (Input.touchCount == 1)
        {
            HandleSingleTouch();
        }
        else if (Input.touchCount == 2)
        {
            HandlePinchToZoom();
        }

        UpdateDistanceDisplay();
    }

    private void UpdateContinuousRaycastDistance()
    {
        // Cast a ray from the camera forward
        Ray ray = new Ray(_arCamera.transform.position, _arCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // Calculate and display the distance to the hit point
            float distanceToHit = hit.distance;
            distanceText.text = $"Distance: {distanceToHit:F2} m";
        }
        else
        {
            // No hit detected
            distanceText.text = "Distance: N/A";
        }
    }

private void UpdateDistanceDisplay()
    {
        if (_objectPlaced && _spawnedObject != null)
        {
            // Calculate the distance from the camera to the object and display it
            float distance = Vector3.Distance(_arCamera.transform.position, _spawnedObject.transform.position);
            distanceText.text = $"Distance: {distance:F2} m";
        }
        else
        {
            distanceText.text = "Distance: N/A";
        }
    }

    private void HandleSingleTouch()
    {
        Touch touch = Input.GetTouch(0);
        if (!_objectPlaced)
        {
            TryPlaceObject(touch);
        }
        else if (_spawnedObject != null && touch.phase == TouchPhase.Moved && !_isPinching)
        {
            MoveObjectToTouchPosition(touch);
        }

        // Detect triple tap
        if (touch.phase == TouchPhase.Ended && !_isPinching)
        {
            if (Time.time - _lastTapTime < _multipleTapMaxDelay)
            {
                _tapCount++;
            }
            else
            {
                _tapCount = 1;
            }

            _lastTapTime = Time.time;

            if (_tapCount == 3)
            {
                StartCoroutine(SinkPrefabIntoPlane());
            }
        }
    }
private void HandlePinchToZoom()
{
    // Make sure we have two touches and the object has been placed
    if (Input.touchCount != 2 || !_objectPlaced)
    {
        return;
    }

    Touch touchZero = Input.GetTouch(0);
    Touch touchOne = Input.GetTouch(1);

    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

    float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
    float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

    float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

    // If we are not already pinching, set the initial scale and flag it
    if (!_isPinching)
    {
        _initialScale = _spawnedObject.transform.localScale;
        _initialPinchDistance = prevTouchDeltaMag;
        _isPinching = true;
    }
    else
    {
        if (Mathf.Abs(deltaMagnitudeDiff) > 0)
        {
            // Calculate the scale factor based on the change in pinch distance
            float pinchRatio = touchDeltaMag / _initialPinchDistance;
            Vector3 scaleChange = _initialScale * pinchRatio;

            // Clamp the scale to ensure it remains between the min and max size bounds
            scaleChange = new Vector3(
                Mathf.Clamp(scaleChange.x, 0.01f, 1.0f),
                Mathf.Clamp(scaleChange.y, 0.01f, 1.0f),
                Mathf.Clamp(scaleChange.z, 0.01f, 1.0f)
            );

            _spawnedObject.transform.localScale = scaleChange;
        }
    }

    // Reset the pinch state when the fingers are lifted
    if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended)
    {
        _isPinching = false;
    }
}




    private bool CheckIfTouchOnObject(Touch touch)
    {
        Ray ray = _arCamera.ScreenPointToRay(touch.position);
        RaycastHit hitObject;
        if (Physics.Raycast(ray, out hitObject))
        {
            return hitObject.transform == _spawnedObject.transform;
        }
        return false;
    }

    private void TryPlaceObject(Touch touch)
    {
        if (_raycastManager.Raycast(touch.position, _raycastHits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            if (touch.phase == TouchPhase.Began)
            {
                _spawnedObject = Instantiate(_spawnablePrefab, _raycastHits[0].pose.position, Quaternion.identity);
                _objectPlaced = true;
            }
        }
    }

    private void MoveObjectToTouchPosition(Touch touch)
    {
        if (_raycastManager.Raycast(touch.position, _raycastHits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            _spawnedObject.transform.position = _raycastHits[0].pose.position;
        }
    }

    private IEnumerator SinkPrefabIntoPlane()
    {
        _isDestroying = true;

        // Example sinking effect: Move prefab down into the plane over 2 seconds
        Vector3 start = _spawnedObject.transform.position;
        Vector3 end = start - new Vector3(0, 0.5f, 0); // Adjust sinking depth as needed

        float duration = 2f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            _spawnedObject.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // After sinking, destroy the prefab
        Destroy(_spawnedObject);
        _spawnedObject = null;
        _objectPlaced = false;
        _isDestroying = false;
        }
}
