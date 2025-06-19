using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ������ ���������� �� �������� � unity 6 ��-�� ����������� Input
public class Old_PersonController : MonoBehaviour
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
    [SerializeField][Range(0, 1)] private float stepInterval = 0.5f; // �������� ����� ������

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

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        player = GetComponent<Transform>();

        // ��������� ���������� ������� (PointerDown)
        EventTrigger trigger = jumpButton.gameObject.AddComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { ClickJump(); });
        trigger.triggers.Add(pointerDown);

        rightFingerId = -1; // -1 ��������, ��� ����� �� �������
        halfScreenWidth = Screen.width / 2;
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

    private void GetTouchInput()
    {
        // ���������� ��� �������� �������
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                // ������ �������
                case TouchPhase.Began:
                    // ���� ������� �������� ������ �� �������� ������ � ������ ����� ��� �� ������������
                    if (touch.position.x > halfScreenWidth && rightFingerId == -1)
                    {
                        rightFingerId = touch.fingerId;
                    }
                    break;
                // ����������� ������
                case TouchPhase.Moved:
                    // ���� ������� ����� ������, ��������� ���� ��� ���������� �������
                    if (touch.fingerId == rightFingerId)
                    {
                        lookInput = touch.deltaPosition * cameraSensitivity * Time.deltaTime;
                    }
                    break;
                // ����� �� ���������
                case TouchPhase.Stationary:
                    // ���� ������� ����� ������, �������� ���� ��� ���������� �������
                    if (touch.fingerId == rightFingerId)
                    {
                        lookInput = Vector2.zero;
                    }
                    break;
                // ������� ��������� / ��������
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    // ���� ���������, ���������� �������������
                    if (touch.fingerId == rightFingerId)
                    {
                        rightFingerId = -1;
                    }
                    break;
            }
        }
    }
}