using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;

// Сокращенные алиасы для удобства работы с системой ввода
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class PersonController : MonoBehaviour
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
    [SerializeField][Range(0, 1)] private float stepInterval = 0.5f;

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

    // Для кэширования данных рейкаста (проверки UI) - Список результатов рейкаста (какие UI-элементы под курсором)
    private List<RaycastResult> raycastResultsCache;
    // Данные о позиции курсора/касания для EventSystem
    private PointerEventData eventDataCache;

    void Start()
    {
        // Включаем расширенную систему обработки касаний (EnhancedTouch)
        // Более точные данные о касаниях
        EnhancedTouchSupport.Enable();

        characterController = GetComponent<CharacterController>();
        player = GetComponent<Transform>();

        // Добавляем обработчик нажатия (PointerDown)
        EventTrigger trigger = jumpButton.gameObject.AddComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { ClickJump(); });
        trigger.triggers.Add(pointerDown);

        halfScreenWidth = Screen.width / 2;
        rightFingerId = -1; // -1 означает, что палец не активен

        // Инициализируем кэшированные объекты для проверки UI:
        raycastResultsCache = new List<RaycastResult>();
        eventDataCache = new PointerEventData(EventSystem.current);
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
                Debug.Log("Я Стою");
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

    // Обработка касаний для управления камерой
    private void GetTouchInput()
    {
        // Список всех активных касаний на экране
        // Массив структур Touch, содержащий данные о каждом пальце на экране
        var touches = Touch.activeTouches;
        
        // есть ли уже зарегистрированное касание для управления камерой?
        if (rightFingerId != -1)
        {
            // Флаг, чтобы отследить, найдено ли касание с rightFingerId
            bool fingerFound = false;

            // Перебираем все активные касания в поисках нашего
            foreach (var touch in touches)
            {
                // Если нашли касание с нужным ID
                if (touch.touchId == rightFingerId)
                {
                    // Подтверждаем, что касание активно
                    fingerFound = true;

                    // Проверяем, началось ли это касание над UI-элементом 
                    // (чтобы игнорировать кнопки/интерфейс)
                    if (touch.phase == TouchPhase.Began && IsPointerOverUIObject(touch.screenPosition))
                    {
                        // Сбрасываем ID, если это UI
                        rightFingerId = -1;
                        break;
                    }

                    // Обрабатываем разные фазы касания:
                    switch (touch.phase)
                    {
                        // Если палец двигается - изменение позиции с прошлого кадра eмножаем на чувствительность и Time.deltaTime для плавности
                        case TouchPhase.Moved:
                            lookInput = touch.delta * cameraSensitivity * Time.deltaTime;
                            break;
                        // Палец касается экрана, но не двигается – обнуляем ввод
                        case TouchPhase.Stationary:
                            lookInput = Vector2.zero;
                            break;
                        // Палец убран с экрана или касание отменено то обнуляем ввод и сбрасываем ID касания
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            lookInput = Vector2.zero;
                            rightFingerId = -1;
                            break;
                    }
                    break;
                }
            }

            // Если касание пропало(например, палец убран), сбрасываем rightFingerId
            if (!fingerFound) rightFingerId = -1;

            // Выходим из метода, т.к. обработка активного касания завершена
            return;
        }

        // Если rightFingerId == -1 (нет активного касания), ищем новое:
        foreach (var touch in touches)
        {
            // Только новые касания, начало касания
            if (touch.phase == TouchPhase.Began)
            {
                // Проверяем, что касание в правой половине экрана не над UI-элементом
                if (touch.screenPosition.x > halfScreenWidth && !IsPointerOverUIObject(touch.screenPosition))
                {
                    // Запоминаем ID нового касания для управления камерой
                    rightFingerId = touch.touchId;
                    break;
                }
            }
        }
    }

    private bool IsPointerOverUIObject(Vector2 pos)
    {
        // Записываем позицию касания в кэшированный объект PointerEventData (чтобы избежать создания нового объекта каждый раз)
        eventDataCache.position = pos;
        // Очищаем список результатов raycast'а (для повторного использования)
        raycastResultsCache.Clear();
        // Делаем raycast через EventSystem, чтобы проверить, есть ли UI под позицией pos
        EventSystem.current.RaycastAll(eventDataCache, raycastResultsCache);
        // Если список результатов не пуст, значит, под касанием есть UI
        return raycastResultsCache.Count > 0;
    }
}