using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;

// ����������� ������ ��� �������� ������ � �������� �����
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class PersonController : MonoBehaviour
{
    [Header("�������� ����������� ���������")]
    [SerializeField][Range(1, 10)] private float speedMove;
    [Header("���� ������")]
    [SerializeField][Range(1, 10)] private float jumpPower;

    [Space(5)]
    [Header("��������")]
    [SerializeField] private Joystick joystick;

    [Space(5)]
    [Header("������ � � ����������������")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField][Range(1, 10)] private float cameraSensitivity;

    [Space(5)]
    [Header("��������� ����")]
    [SerializeField][Range(0, 1)] private float amplitude;
    [SerializeField][Range(0, 1)] private float stepInterval = 0.5f;

    [Space(5)]
    [Header("���� ����")]
    [SerializeField] private AudioSource walkSound;

    [Space(5)]
    [Header("������ ������")]
    [SerializeField] private Button jumpButton;

    private float stepTimer = 0f; // ������ ����� ������
    private float gravityForce; // ������� ����������
    private Vector3 moveVector; // ������ �������� ���������
    private CharacterController characterController; 
    private int rightFingerId; // id ������ ��� ���������� �������
    private float halfScreenWidth; // �������� ������ ������
    private Vector2 lookInput; // ������� ������ ��� �������� ������
    private float cameraPitch; // ���� ������� ������ �� ���������
    private float walkTimerCounter; // ������� ������� ��� �������� ����
    private bool isWalking; // ���� ��������� ��������� (���� �� ��)
    private Transform player;

    // ��� ����������� ������ �������� (�������� UI) - ������ ����������� �������� (����� UI-�������� ��� ��������)
    private List<RaycastResult> raycastResultsCache;
    // ������ � ������� �������/������� ��� EventSystem
    private PointerEventData eventDataCache;

    void Start()
    {
        // �������� ����������� ������� ��������� ������� (EnhancedTouch)
        // ����� ������ ������ � ��������
        EnhancedTouchSupport.Enable();

        characterController = GetComponent<CharacterController>();
        player = GetComponent<Transform>();

        // ��������� ���������� ������� (PointerDown)
        EventTrigger trigger = jumpButton.gameObject.AddComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { ClickJump(); });
        trigger.triggers.Add(pointerDown);

        halfScreenWidth = Screen.width / 2;
        rightFingerId = -1; // -1 ��������, ��� ����� �� �������

        // �������������� ������������ ������� ��� �������� UI:
        raycastResultsCache = new List<RaycastResult>();
        eventDataCache = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        GetTouchInput();

        // ���� ������� ����� ��� ���������� �������
        if (rightFingerId != -1)
        {
            LookAround();
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        GameGravity();
    }

    private void MovePlayer()
    {
        if (!characterController.enabled) return;

        moveVector = Vector3.zero;
        // �������� ���� � ���������
        moveVector.x = joystick.Horizontal;
        moveVector.z = joystick.Vertical;
        moveVector.y = gravityForce;

        // ����������� ������ �������� � ��������� ���������� ���������
        moveVector = transform.right * moveVector.x + transform.forward * moveVector.z + transform.up * moveVector.y;

        // ������� ��������� � ������ �������� � ������� �����
        characterController.Move(moveVector * speedMove * Time.deltaTime);

        // ���� �������� ������� (�������� ��������)
        if (Mathf.Abs(joystick.Horizontal) > 0.1f || Mathf.Abs(joystick.Vertical) > 0.1f)
        {
            // ���� ������ ������ ��������
            if (!isWalking)
            {
                walkTimerCounter = 0f;
                isWalking = true;
            }
            // �������� ���� (����������� �����-����)
            // ������ ��� �������� ����: ����������� �����, ��������� � ���������� �����
            walkTimerCounter += Time.deltaTime;

            // ��������� ������ ���� (����������� �����-����):
            // 1. Mathf.Sin() ������� ������� ������������� �������� (�� -1 �� 1)
            // 2. �������� walkTimerCounter �� (speedMove * 2.0f) - ��� ���� ��������, ��� ������� ���������
            // 3. �������� ��������� �� amplitude (���������) - ���������� ������ �������������
            float walkEffect = Mathf.Sin(walkTimerCounter * (speedMove * 2.0f)) * amplitude;

            // ��������� ������ ���� � ������� ���������:
            // 1. ��������� ������� X � Z ����������
            // 2. � Y ���������� ��������� ����������� walkEffect
            player.localPosition = new Vector3(player.localPosition.x, player.localPosition.y + walkEffect, player.localPosition.z);

            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                // ������������� ���� ����, ���� �������� �� ����� � ���� �� ������
                if (isWalking && characterController.isGrounded && walkSound != null && !walkSound.isPlaying)
                {
                    walkSound.Play();
                }
            }
        }
        else
        {
            // ���� �������� �����������
            if (isWalking)
            {
                Debug.Log("� ����");
                // ��������� �������
                SavePositionPlayer.Instance.SavePlayerPosition();
                isWalking = false;
            }
            // ������������� ���� �����, ���� �� ������
            if (!isWalking && walkSound != null && walkSound.isPlaying)
            {
                walkSound.Stop();
            }
        }
    }

    private void GameGravity()
    {
        // ���� �������� �� �����
        if (!characterController.isGrounded)
        {
            // ������ ��������� ���� ����������
            gravityForce -= 10f * Time.deltaTime;
        }
        else
        {
            // � ������� - ���������� ��������� ����������
            gravityForce = -1f;
        }
    }

    private void ClickJump()
    {
        // ������� ������ ���� �������� �� �����
        if (characterController.isGrounded)
        {
            gravityForce = jumpPower;
        }
    }

    private void LookAround()
    {
        // ������������ ���� ������� ������ �� ���������
        cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
        // ��������� �������� ������ �� ���������
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        // ������� ��������� �� �����������
        transform.Rotate(transform.up, lookInput.x);
    }

    // ��������� ������� ��� ���������� �������
    private void GetTouchInput()
    {
        // ������ ���� �������� ������� �� ������
        // ������ �������� Touch, ���������� ������ � ������ ������ �� ������
        var touches = Touch.activeTouches;
        
        // ���� �� ��� ������������������ ������� ��� ���������� �������?
        if (rightFingerId != -1)
        {
            // ����, ����� ���������, ������� �� ������� � rightFingerId
            bool fingerFound = false;

            // ���������� ��� �������� ������� � ������� ������
            foreach (var touch in touches)
            {
                // ���� ����� ������� � ������ ID
                if (touch.touchId == rightFingerId)
                {
                    // ������������, ��� ������� �������
                    fingerFound = true;

                    // ���������, �������� �� ��� ������� ��� UI-��������� 
                    // (����� ������������ ������/���������)
                    if (touch.phase == TouchPhase.Began && IsPointerOverUIObject(touch.screenPosition))
                    {
                        // ���������� ID, ���� ��� UI
                        rightFingerId = -1;
                        break;
                    }

                    // ������������ ������ ���� �������:
                    switch (touch.phase)
                    {
                        // ���� ����� ��������� - ��������� ������� � �������� ����� e������� �� ���������������� � Time.deltaTime ��� ���������
                        case TouchPhase.Moved:
                            lookInput = touch.delta * cameraSensitivity * Time.deltaTime;
                            break;
                        // ����� �������� ������, �� �� ��������� � �������� ����
                        case TouchPhase.Stationary:
                            lookInput = Vector2.zero;
                            break;
                        // ����� ����� � ������ ��� ������� �������� �� �������� ���� � ���������� ID �������
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            lookInput = Vector2.zero;
                            rightFingerId = -1;
                            break;
                    }
                    break;
                }
            }

            // ���� ������� �������(��������, ����� �����), ���������� rightFingerId
            if (!fingerFound) rightFingerId = -1;

            // ������� �� ������, �.�. ��������� ��������� ������� ���������
            return;
        }

        // ���� rightFingerId == -1 (��� ��������� �������), ���� �����:
        foreach (var touch in touches)
        {
            // ������ ����� �������, ������ �������
            if (touch.phase == TouchPhase.Began)
            {
                // ���������, ��� ������� � ������ �������� ������ �� ��� UI-���������
                if (touch.screenPosition.x > halfScreenWidth && !IsPointerOverUIObject(touch.screenPosition))
                {
                    // ���������� ID ������ ������� ��� ���������� �������
                    rightFingerId = touch.touchId;
                    break;
                }
            }
        }
    }

    private bool IsPointerOverUIObject(Vector2 pos)
    {
        // ���������� ������� ������� � ������������ ������ PointerEventData (����� �������� �������� ������ ������� ������ ���)
        eventDataCache.position = pos;
        // ������� ������ ����������� raycast'� (��� ���������� �������������)
        raycastResultsCache.Clear();
        // ������ raycast ����� EventSystem, ����� ���������, ���� �� UI ��� �������� pos
        EventSystem.current.RaycastAll(eventDataCache, raycastResultsCache);
        // ���� ������ ����������� �� ����, ������, ��� �������� ���� UI
        return raycastResultsCache.Count > 0;
    }
}