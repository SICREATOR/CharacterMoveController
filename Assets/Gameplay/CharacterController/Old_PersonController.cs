using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Данный контроллер не работает в unity 6 из-за устаревшего Input
public class Old_PersonController : MonoBehaviour
{
    [Header("Скорость перемещения персонажа")]
    [SerializeField][Range(1, 10)] private float speedMove;
    [Header("Сила прыжка")]
    [SerializeField][Range(1, 10)] private float jumpPower;

    [Space(5)]

    [Header("Джойстик")]
    [SerializeField] private Joystick joystick;

    [Space(5)]

    [Header("Камера и её чувствительность")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField][Range(1, 10)] private float cameraSensitivity;

    [Space(5)]

    [Header("Амплитуда шага")]
    [SerializeField][Range(0, 1)] private float amplitude;
    [SerializeField][Range(0, 1)] private float stepInterval = 0.5f; // Интервал между шагами

    [Space(5)]

    [Header("Звук шага")]
    [SerializeField] private AudioSource walkSound;

    [Space(5)]
    [Header("Кнопка прыжка")]
    [SerializeField] private Button jumpButton;

    private float stepTimer = 0f; // Таймер между шагами
    private float gravityForce; // Текущая гравитация
    private Vector3 moveVector; // Вектор движения персонажа
    private CharacterController characterController;
    private int rightFingerId; // id пальца для управления камерой
    private float halfScreenWidth; // Половина ширины экрана
    private Vector2 lookInput; // Входные данные для вращения камеры
    private float cameraPitch; // Угол наклона камеры по вертикали
    private float walkTimerCounter; // Счетчик времени для анимации шага
    private bool isWalking; // Флаг состояния персонажа (идет ли он)
    private Transform player;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        player = GetComponent<Transform>();

        // Добавляем обработчик нажатия (PointerDown)
        EventTrigger trigger = jumpButton.gameObject.AddComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { ClickJump(); });
        trigger.triggers.Add(pointerDown);

        rightFingerId = -1; // -1 означает, что палец не активен
        halfScreenWidth = Screen.width / 2;
    }

    private void Update()
    {
        GetTouchInput();
        // Если активен палец для управления камерой
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
        // Получаем ввод с джойстика
        moveVector.x = joystick.Horizontal;
        moveVector.z = joystick.Vertical;
        moveVector.y = gravityForce;

        // Преобразуем вектор движения в локальные координаты персонажа
        moveVector = transform.right * moveVector.x + transform.forward * moveVector.z + transform.up * moveVector.y;

        // Двигаем персонажа с учетом скорости и времени кадра
        characterController.Move(moveVector * speedMove * Time.deltaTime);

        // Если джойстик активен (персонаж движется)
        if (Mathf.Abs(joystick.Horizontal) > 0.1f || Mathf.Abs(joystick.Vertical) > 0.1f)
        {
            // Если только начали движение
            if (!isWalking)
            {
                walkTimerCounter = 0f;
                isWalking = true;
            }
            // Анимация шага (покачивание вверх-вниз)
            // Таймер для анимации шага: накапливаем время, прошедшее с последнего кадра
            walkTimerCounter += Time.deltaTime;

            // Вычисляем эффект шага (покачивание вверх-вниз):
            // 1. Mathf.Sin() создает плавное колебательное движение (от -1 до 1)
            // 2. Умножаем walkTimerCounter на (speedMove * 2.0f) - чем выше скорость, тем быстрее колебания
            // 3. Умножаем результат на amplitude (амплитуда) - определяет высоту подпрыгивания
            float walkEffect = Mathf.Sin(walkTimerCounter * (speedMove * 2.0f)) * amplitude;

            // Применяем эффект шага к позиции персонажа:
            // 1. Сохраняем текущие X и Z координаты
            // 2. К Y координате добавляем вычисленный walkEffect
            player.localPosition = new Vector3(player.localPosition.x, player.localPosition.y + walkEffect, player.localPosition.z);

            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                // Воспроизводим звук шага, если персонаж на земле и звук не играет
                if (isWalking && characterController.isGrounded && walkSound != null && !walkSound.isPlaying)
                {
                    walkSound.Play();
                }
            }
        }
        else
        {
            // Если персонаж остановился
            if (isWalking)
            {
                // Сохраняем позицию
                SavePositionPlayer.Instance.SavePlayerPosition();
                isWalking = false;
            }
            // Останавливаем звук шагов, если он играет
            if (!isWalking && walkSound != null && walkSound.isPlaying)
            {
                walkSound.Stop();
            }
        }
    }

    private void GameGravity()
    {
        // Если персонаж на земле
        if (!characterController.isGrounded)
        {
            // Плавно уменьшаем силу гравитации
            gravityForce -= 10f * Time.deltaTime;
        }
        else
        {
            // В воздухе - постоянная небольшая гравитация
            gravityForce = -1f;
        }
    }

    private void ClickJump()
    {
        // Прыгаем только если персонаж на земле
        if (characterController.isGrounded)
        {
            gravityForce = jumpPower;
        }
    }

    private void LookAround()
    {
        // Ограничиваем угол наклона камеры по вертикали
        cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
        // Применяем вращение камеры по вертикали
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        // Вращаем персонажа по горизонтали
        transform.Rotate(transform.up, lookInput.x);
    }

    private void GetTouchInput()
    {
        // Перебираем все активные касания
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                // Начало касания
                case TouchPhase.Began:
                    // Если касание началось справа от середины экрана и правый палец еще не используется
                    if (touch.position.x > halfScreenWidth && rightFingerId == -1)
                    {
                        rightFingerId = touch.fingerId;
                    }
                    break;
                // Перемещение пальца
                case TouchPhase.Moved:
                    // Если текущий палец правый, обновляем вход для управления камерой
                    if (touch.fingerId == rightFingerId)
                    {
                        lookInput = touch.deltaPosition * cameraSensitivity * Time.deltaTime;
                    }
                    break;
                // Палец не двигается
                case TouchPhase.Stationary:
                    // Если текущий палец правый, обнуляет вход для управления камерой
                    if (touch.fingerId == rightFingerId)
                    {
                        lookInput = Vector2.zero;
                    }
                    break;
                // Касание завершено / отменено
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    // Если завершено, сбрасываем идентификатор
                    if (touch.fingerId == rightFingerId)
                    {
                        rightFingerId = -1;
                    }
                    break;
            }
        }
    }
}