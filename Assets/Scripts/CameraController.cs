﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public delegate void CameraEventCallback();

public enum CameraModeEnum {FreeLook, MoveBehindPanTo, Locked};

public class CameraController : MonoBehaviour
{
    public CameraModeEnum mode = CameraModeEnum.FreeLook;

    public float movementSpeed = 10f;
	public float fastMovementSpeed = 100f;
	public float freeLookSensitivity = 3f;
	public float zoomSensitivity = 10f;
    public float fastZoomSensitivity = 50f;
    public float xLock = 1f;
	private bool looking = false;

    public Transform targetObject;
    public float moveBehindDistance = 10f;
    public float moveBehindHeight = 10f;
    public float moveBehindSpeed = 1f;
    public float panToSpeed = 1;
    public float finishedDelta = 0.0001f;

    public CameraEventCallback moveBehindPanToCallback;

    void Update()
	{
        switch(mode)
        {
            case CameraModeEnum.FreeLook:
                FreeLook();
                break;

            case CameraModeEnum.MoveBehindPanTo:
                MoveBehindPanTo();
                break;

            case CameraModeEnum.Locked:
                break;

            default:
                break;
        }
	}

    void MoveBehindPanTo()
    {
        if(targetObject != null)
        {
            // Get final Position
            Vector3 finalPosition = targetObject.position - (targetObject.forward * moveBehindDistance) + (targetObject.up * moveBehindHeight);

            // Move to final position
            Vector3 newPosition = Vector3.MoveTowards(this.transform.position, finalPosition, moveBehindSpeed * Time.deltaTime);

            float positionDelta = (transform.position - newPosition).magnitude;

            transform.position = newPosition;

            // Look at target object

            Vector3 targetDirection = targetObject.position - transform.position;
            Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, panToSpeed * Time.deltaTime, panToSpeed);
            Quaternion newRotaion = Quaternion.LookRotation(newDirection);

            float rotationDelta = Quaternion.Angle(newRotaion, transform.rotation) * Mathf.Deg2Rad;

            transform.rotation = newRotaion;

            // Check if finished

            if (rotationDelta < finishedDelta && positionDelta < finishedDelta)
            {
                moveBehindPanToCallback?.Invoke();
            }
        }
    }

    void FreeLook()
    {
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.position = transform.position + (transform.forward * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            transform.position = transform.position + (-transform.forward * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.position = transform.position + (transform.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.E))
        {
            transform.position = transform.position + (-transform.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.PageUp))
        {
            transform.position = transform.position + (Vector3.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.PageDown))
        {
            transform.position = transform.position + (-Vector3.up * movementSpeed * Time.deltaTime);
        }

        if (looking)
        {
            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;

            if (newRotationY > 90f - xLock && newRotationY < 180f)
            {
                newRotationY = 90f - xLock;
            }


            if (newRotationY < 270f + xLock && newRotationY > 180f)
            {
                newRotationY = 270f + xLock;
            }

            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis != 0)
        {
            var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
            transform.position = transform.position + transform.forward * axis * zoomSensitivity;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartLooking();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            StopLooking();
        }
    }

	void OnDisable()
	{
		StopLooking();
	}

	public void StartLooking()
	{
		looking = true;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	public void StopLooking()
	{
		looking = false;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}
}