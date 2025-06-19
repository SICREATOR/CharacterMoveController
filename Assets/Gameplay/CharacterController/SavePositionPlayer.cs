using System.Collections;
using UnityEngine;

public class SavePositionPlayer : MonoBehaviour
{
    [SerializeField] private Vector3 defaultPlayerPosition;

    public static SavePositionPlayer Instance;
    private CharacterController characterController;

    void Start()
    {
        Instance = this;
        characterController = GetComponent<CharacterController>();
        LoadPlayerPosition();
    }

    public void SavePlayerPosition()
    {
        PlayerPrefs.SetFloat("PlayerPositionX", characterController.transform.position.x);
        PlayerPrefs.SetFloat("PlayerPositionY", characterController.transform.position.y);
        PlayerPrefs.SetFloat("PlayerPositionZ", characterController.transform.position.z);
        PlayerPrefs.SetFloat("PlayerRotationY", characterController.transform.eulerAngles.y);
        PlayerPrefs.Save();
    }

    private void LoadPlayerPosition()
    {
        if (PlayerPrefs.HasKey("PlayerPositionX"))
        {
            float x = PlayerPrefs.GetFloat("PlayerPositionX");
            float y = PlayerPrefs.GetFloat("PlayerPositionY");
            float z = PlayerPrefs.GetFloat("PlayerPositionZ");
            float ry = PlayerPrefs.GetFloat("PlayerRotationY");

            StartCoroutine(ChangePositionAndRotation(new Vector3(x, y, z), ry));
        }
        else
        {
            StartCoroutine(ChangePositionAndRotation(defaultPlayerPosition, 0f));
        }
    }

    // Ѕкз корутины, он не перемещаетс€ в ту точку в которой сохранен
    private IEnumerator ChangePositionAndRotation(Vector3 vector3, float rotationY)
    {
        characterController.enabled = false;
        characterController.transform.position = vector3;

        // ”станавливаем поворот по оси Y, сохран€€ остальные оси как есть
        characterController.transform.eulerAngles = new Vector3(
            characterController.transform.eulerAngles.x,
            rotationY,
            characterController.transform.eulerAngles.z
        );

        yield return null;
        characterController.enabled = true;
    }
}
