using UnityEngine;

public class GearRotation : MonoBehaviour
{
    [Tooltip("Скорость вращения в градусах в секунду")]
    public float speed = 50f;

    [Tooltip("Инвертировать направление (для соседней шестерни)")]
    public bool invertDirection = false;

    public enum Axis { X, Y, Z }
    public Axis rotationAxis = Axis.Y;

    void Update()
    {
        float direction = invertDirection ? -1f : 1f;
        float rotationAmount = speed * direction * Time.deltaTime;

        switch (rotationAxis)
        {
            case Axis.X: transform.Rotate(rotationAmount, 0, 0); break;
            case Axis.Y: transform.Rotate(0, rotationAmount, 0); break;
            case Axis.Z: transform.Rotate(0, 0, rotationAmount); break;
        }
    }
}